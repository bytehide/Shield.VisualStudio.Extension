using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ShieldVSExtension.Common;
using ShieldVSExtension.Common.Helpers;
using ShieldVSExtension.ViewModels;

namespace ShieldVSExtension.UI.UserControls;

public partial class ActionBarControl
{
    public ActionBarControl()
    {
        InitializeComponent();
        ViewModelBase.IsMsbuilderInstalledHandler += OnMsbuilderInstalled;
        ViewModelBase.VsixVersionHandler += OnVsixVersion;
        ViewModelBase.MsbuilderInstallHandler += OnMsbuilderInstall;

        // Unloaded += (_, _) =>
        // {
        //     ViewModelBase.IsMsbuilderInstalledHandler -= OnMsbuilderInstalled;
        //     ViewModelBase.VsixVersionHandler -= OnVsixVersion;
        //     ViewModelBase.MsbuilderInstallHandler -= OnMsbuilderInstall;
        // };
    }

    private void OnMsbuilderInstall() => _ = ToggleProtectionAsync();

    private void OnMsbuilderInstalled(bool installed) => Dispatcher.Invoke(() => ActiveButton.IsChecked = installed);

    private void OnVsixVersion(VersionInfo info)
    {
        ViewModelBase.VsixVersionHandler -= OnVsixVersion;

        if (info is { ForceUpdate: false, CanUpdate: false })
        {
            ActiveButton.IsEnabled = true;
            VersionRoot.Visibility = Visibility.Collapsed;
            return;
        }

        VersionRoot.Background = info.ForceUpdate
            ? new SolidColorBrush(Colors.Crimson) { Opacity = 0.7 }
            : new SolidColorBrush(Colors.IndianRed) { Opacity = 0.7 };

        ActiveButton.IsEnabled = !info.ForceUpdate;

        // VersionRoot.ToolTip = info.Message;
        VersionMessageBox.Text = info.Message;

        VersionRoot.Visibility = Visibility.Visible;
    }

    private void ActiveOnChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { IsInitialized: true, IsMouseOver: true }) return;

        ToggleProtectionAsync().GetAwaiter();
    }

    private async Task ToggleProtectionAsync()
    {
        ActiveButton.Visibility = Visibility.Collapsed;
        LoaderControl.Visibility = Visibility.Visible;

        await Task.Run(() =>
        {
            var helper = new NugetHelper();
            var installed = helper.IsPackageInstalledAsync(Payload.Project, NugetHelper.PackageId).GetAwaiter().GetResult();

            var result = !installed
                ? helper.InstallPackageAsync(Payload.Project)
                : helper.UninstallPackageAsync(Payload.Project);

            var proceed = result.GetAwaiter().GetResult() ? !installed : installed;

            Payload.Installed = proceed;
            ViewModelBase.IsMsbuilderInstalledHandler.Invoke(proceed);
        });

        ActiveButton.Visibility = Visibility.Visible;
        LoaderControl.Visibility = Visibility.Collapsed;
    }

    #region Commands

    public ProjectViewModel Payload
    {
        get => (ProjectViewModel)Dispatcher.Invoke(() => GetValue(PayloadProperty));
        set => Dispatcher.Invoke(() => SetValue(PayloadProperty, value));
    }

    public static readonly DependencyProperty PayloadProperty = DependencyProperty.Register(
        nameof(Payload),
        typeof(ProjectViewModel),
        typeof(ActionBarControl),
        new PropertyMetadata(null));

    #endregion
}