using System;
using ShieldVSExtension.Common.Helpers;
using ShieldVSExtension.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace ShieldVSExtension.UI.Views;

/// <summary>
/// Interaction logic for DisabledControl.xaml
/// </summary>
public partial class DisabledControl
{
    public DisabledControl()
    {
        InitializeComponent();

        Loaded += LoadData;

        ViewModelBase.ProjectChangedHandler += OnProjectChanged;
        ViewModelBase.IsMsbuilderInstalledHandler += OnMsbuilderInstalled;

        // Unloaded += (_, _) => ViewModelBase.ProjectChangedHandler -= OnProjectChanged;
    }

    // TODO improve this
    private void LoadData(object sender, RoutedEventArgs e)
    {
        var helper = new NugetHelper();
        var installed = helper.IsPackageInstalledAsync(Payload.Project, NugetHelper.PackageId).GetAwaiter().GetResult();

        Dispatcher.Invoke(() => EnableButton.IsEnabled = !installed);
    }

    private void OnMsbuilderInstalled(bool installed)
    {
        Dispatcher.Invoke(() => EnableButton.IsEnabled = !installed);
    }

    private void OnProjectChanged(ProjectViewModel payload)
    {
        if (payload == null) return;

        // Payload = payload;
        // var buff = payload.Name;

        try
        {
            // var helper = new NugetHelper();
            // var installed = helper.IsPackageInstalled(payload.Project, NugetHelper.PackageId, null);

            Dispatcher.Invoke(() => EnableButton.IsEnabled = true);
            // Payload.Installed = installed;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }

    private void EnableButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        try
        {
            // var helper = new NugetHelper();
            // var installed = helper.IsPackageInstalled(Payload.Project, NugetHelper.PackageId, null);

            button.IsEnabled = false;

            // if (installed)
            // {
            //     button.IsEnabled = false;
            //     return;
            // }

            ViewModelBase.MsbuilderInstallHandler.Invoke();
        }
        catch (Exception exception)
        {
            button.IsEnabled = true;
            Console.WriteLine(exception);
        }
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
        typeof(DisabledControl),
        new PropertyMetadata(null));

    #endregion
}