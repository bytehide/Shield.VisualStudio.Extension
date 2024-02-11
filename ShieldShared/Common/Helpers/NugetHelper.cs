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

    public Task InstallPackageAsync(Project project)
    {
        try
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            // var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            _packageInstaller = componentModel.GetService<IVsPackageInstaller2>();

            if (_packageInstaller == null)
            {
                MessageBox.Show(
                    "Package installer is not available. Please make sure that NuGet is installed and enabled in Visual Studio.",
                    "Error"
                );

                return Task.CompletedTask;
            }

            // if (IsPackageInstalled(project, PackageId, null)) return Task.CompletedTask;

            _packageInstaller?.InstallLatestPackage(
                source: null,
                project,
                PackageId,
                includePrerelease: false,
                ignoreDependencies: false
            );

            // MessageBox.Show(@"Package installed successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during package installation: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public bool IsPackageInstalled(Project project, string packageId, SemanticVersion packageVersion)
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

    public Task UninstallPackageAsync(Project project)
    {
        try
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            // var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            _packageUninstaller = componentModel.GetService<IVsPackageUninstaller>();

            if (!IsPackageInstalled(project, PackageId, null))
            {
                // MessageBox.Show(@"Package not installed");
                return Task.CompletedTask;
            }

            if (_packageUninstaller == null)
            {
                MessageBox.Show(
                    "Package installer is not available. Please make sure that NuGet is installed and enabled in Visual Studio.",
                    "Error"
                );

                return Task.CompletedTask;
            }

            _packageUninstaller?.UninstallPackage(project, PackageId, false);
            // MessageBox.Show(@"Package uninstalled successfully");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($@"Error during package uninstallation: {ex.Message}");
            Debug.WriteLine($"Error during package uninstallation: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}