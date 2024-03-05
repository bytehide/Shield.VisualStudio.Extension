using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShieldVSExtension.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ShieldSolutionConfiguration = ShieldVSExtension.Common.Configuration.SolutionConfiguration;
using Task = System.Threading.Tasks.Task;
using ShieldVSExtension.UI;
using BuildEvents = EnvDTE.BuildEvents;
using OutputWindowPane = EnvDTE.OutputWindowPane;
using SolutionEvents = EnvDTE.SolutionEvents;

namespace ShieldVSExtension;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
/// </summary>
/// <remarks>
/// <para>
/// The minimum requirement for a class to be considered a valid package for Visual Studio
/// is to implement the IVsPackage interface and register itself with the shell.
/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
/// to do it: it derives from the Package class that provides the implementation of the
/// IVsPackage interface and uses the registration attributes defined in the framework to
/// register itself and its components with the shell. These attributes tell the pkgdef creation
/// utility what data to put into .pkgdef file.
/// </para>
/// <para>
/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
/// </para>
/// </remarks>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideToolWindow(typeof(AppWindow))]
public sealed class ShieldVsExtensionPackage : AsyncPackage
{
    /// <summary>
    /// ShieldVSExtensionPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "311f3401-ccf0-489a-b402-97528dc6b439";

    private const string ShieldConfiguration = "ShieldConfigurationPkg";

    internal static ShieldSolutionConfiguration Configuration { get; set; }

    private OutputWindowPane Pane { get; set; }

    // private OutputWindowPane OutputPane { get; set; }

    private vsBuildAction CurrentBuildAction { get; set; }

    private DTE2 Dte { get; set; }

    private BuildEvents _buildEvents;

    private SolutionEvents _solutionEvents;

    private ErrorListProvider ErrorListProvider { get; set; }

    // private ShieldExtensionConfiguration ExtensionConfiguration { get; set; }

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    /// <param name="progress">A provider for progress updates.</param>
    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    protected override async Task InitializeAsync(CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        _ = AppDomain.CurrentDomain.GetAssemblies().ToList();

        // When initialized asynchronously, the current thread may be a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        await Enable.InitializeAsync(this);
        await MainWindowCommand.InitializeAsync(this);

        Dte = await GetServiceAsync(typeof(DTE)) as DTE2;
        if (Dte == null)
        {
            Debug.Fail("DTE service is null");
            throw new ArgumentNullException(nameof(Dte));
        }

        Pane = Dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("ByteHide Shield");

        ErrorListProvider = new ErrorListProvider(this);

        _solutionEvents = Dte.Events.SolutionEvents;

        _buildEvents = Dte.Events.BuildEvents;

        _buildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;

        _buildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;

        var isSolutionLoaded = await IsSolutionLoadedAsync();

        _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;

        AddOptionKey(ShieldConfiguration);

        var solutionPersistenceService = (IVsSolutionPersistence)await GetServiceAsync(typeof(IVsSolutionPersistence));
        if (solutionPersistenceService == null)
        {
            Debug.Fail("SolutionPersistenceService is null");
            throw new ArgumentNullException(nameof(solutionPersistenceService));
        }

        solutionPersistenceService.LoadPackageUserOpts(this, ShieldConfiguration);

        if (isSolutionLoaded)
        {
            SolutionEventsOnOpened();
        }

        _solutionEvents.Opened += SolutionEventsOnOpened;
    }

    private async Task<bool> IsSolutionLoadedAsync()
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync();

        if (await GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solService)
        {
            throw new ArgumentNullException(nameof(solService));
        }

        ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var value));

        return value is true;
    }

    private void ActivePane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Pane.Activate();
    }

    private static void SolutionEvents_AfterClosing() => Enable.Command.Visible = false;

    internal static void UpdateExtensionEnabled(bool? isEnabled = null)
    {
        if (isEnabled.HasValue)
        {
            Enable.Command.Checked = isEnabled.Value;
        }
        else
        {
            Enable.Command.Checked = Configuration is { IsEnabled: true };
        }
    }

    private static void SolutionEventsOnOpened()
    {
        Enable.Command.Visible = true;
        MainWindowCommand.Command.Visible = true;

        Configuration ??= new ShieldSolutionConfiguration();

        UpdateExtensionEnabled();
    }

    public void BuildEvents_OnBuildProjConfigDone(string projectName, string projectConfig, string platform,
        string solutionConfig, bool success)
    {
        _ = JoinableTaskFactory.RunAsync(async delegate
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

            if (Configuration.BuildConfiguration != "*" && !string.Equals(projectConfig,
                    Configuration.BuildConfiguration, StringComparison.CurrentCultureIgnoreCase))
                return;

            if (!success || Configuration is not { IsEnabled: true }) return;

            if (CurrentBuildAction != vsBuildAction.vsBuildActionBuild &&
                CurrentBuildAction != vsBuildAction.vsBuildActionRebuildAll) return;


            ActivePane();
        });
    }

    private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        CurrentBuildAction = action;
        Pane.Clear();

        ErrorListProvider.Tasks.Clear();
    }

    #endregion
}