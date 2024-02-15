using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet;
using NuGet.VisualStudio;
using MessageBox = System.Windows.MessageBox;

namespace ShieldVSExtension.Common.Helpers;

[Export(typeof(NugetHelper))]
internal class NugetHelper
{
    public static readonly string PackageId = "Bytehide.Shield.Integration";
    // public static readonly SemanticVersion PackageVersion = SemanticVersion.Parse("2.1.0");
    // private static readonly SemanticVersion PackageVersion = new("1.0.0");

    [Import(typeof(IVsPackageInstaller2))] private IVsPackageInstaller2 _packageInstaller;

    [Import(typeof(IVsPackageUninstaller))]
    private IVsPackageUninstaller _packageUninstaller;

    public async Task<bool> InstallPackageAsync(Project project)
    {
        try
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _packageInstaller = componentModel?.GetService<IVsPackageInstaller2>();

            if (_packageInstaller == null)
            {
                MessageBox.Show(
                    "Package installer is not available. Please make sure that NuGet is installed and enabled in Visual Studio.",
                    "Error"
                );
                return false;
            }

            if (await IsPackageInstalledAsync(project, PackageId))
            {
                return true;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _packageInstaller.InstallLatestPackage(null, project, PackageId, false, false);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during package installation: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UninstallPackageAsync(Project project)
    {
        try
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _packageUninstaller = componentModel?.GetService<IVsPackageUninstaller>();

            if (_packageUninstaller == null)
            {
                MessageBox.Show(
                    "Package uninstaller is not available. Please make sure that NuGet is installed and enabled in Visual Studio.",
                    "Error"
                );

                return false;
            }

            if (!await IsPackageInstalledAsync(project, PackageId))
            {
                return true;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _packageUninstaller.UninstallPackage(project, PackageId, false);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during package uninstallation: {ex.Message}");
            return true;
        }
    }

    public async Task<bool> IsPackageInstalledAsync(Project project, string packageId)
    {
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var installerServices = componentModel?.GetService<IVsPackageInstallerServices>();

            return installerServices != null && IsPackageInstalled(project, packageId, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking if package is installed: {ex.Message}");
            return false;
        }
    }

    private static bool IsPackageInstalled(Project project, string packageId, SemanticVersion packageVersion)
    {
        try
        {
            //  var packageReferences = project.GetReferences();
            //  foreach (var packageReference in packageReferences)
            //  {
            //      if (packageReference.reference != packageId) continue;
            // 
            //      var packageName = packageReference.reference;
            //      var version = packageReference.strongInfo;
            // 
            //      if (SemanticVersion.Parse(version) == packageVersion)
            //      {
            //          return true;
            //      }
            //  }

            // IAsyncServiceProvider asyncServiceProvider = this;
            // var brokeredServiceContainer = await asyncServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            // IServiceBroker serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();
            // INuGetProjectService nugetProjectService = await serviceBroker.GetProxyAsync<INuGetProjectService>(NuGetServices.NuGetProjectServiceV1);
            // 
            // InstalledPackagesResult installedPackagesResult;
            // using (nugetProjectService as IDisposable)
            // {
            //     installedPackagesResult = await nugetProjectService.GetInstalledPackagesAsync(projectGuid, cancellationToken);
            // }

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            var installed = packageVersion != null
                ? installerServices.IsPackageInstalled(project, packageId, packageVersion)
                : installerServices.IsPackageInstalled(project, packageId);

            return installed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during checking installation: {ex.Message}");
            return false;
        }
    }
}