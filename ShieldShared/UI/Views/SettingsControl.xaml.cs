using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using ShieldVSExtension.Common;
using ShieldVSExtension.Common.Extensions;
using ShieldVSExtension.Common.Helpers;
using ShieldVSExtension.Common.Models;
using ShieldVSExtension.Common.Validators;
using ShieldVSExtension.Storage;
using ShieldVSExtension.ViewModels;
using Globals = ShieldVSExtension.Common.Globals;
using Task = System.Threading.Tasks.Task;

namespace ShieldVSExtension.UI.Views;

/// <summary>
/// Interaction logic for SettingsControl.xaml
/// </summary>
public partial class SettingsControl
{
    public SecureLocalStorage LocalStorage { get; set; }

    // public ObservableCollection<string> SolutionConfigurations { get; set; }
    // private readonly SettingsViewModel _vm;

    public SettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        ViewModelBase.ProjectChangedHandler += OnProjectChanged;
        Unloaded += OnFree;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Task.Delay(100).ConfigureAwait(false).GetAwaiter()
        .OnCompleted(() => LoadDataAsync().GetAwaiter());

    private void OnProjectChanged(ProjectViewModel payload)
    {
        if (payload == null) return;

        Payload = payload;
        LoadDataAsync().GetAwaiter();
    }

    private async Task LoadDataAsync()
    {
        if (Payload == null) return;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        LocalStorage = new SecureLocalStorage(new CustomLocalStorageConfig(null, Globals.ShieldLocalStorageName)
            .WithDefaultKeyBuilder());

        var data = LocalStorage.Get<ShieldConfiguration>($"{Payload.Project.UniqueName.ToUuid()}");

        ProjectNameBox.Text = data?.Name ?? $"Configuration {Payload.Project.Name}";
        ProjectTokenBox.Text = data?.ProjectToken ?? string.Empty;
        SecretBox.Password = data?.ProtectionSecret ?? string.Empty;
        StatusToggle.IsChecked = data?.Enabled ?? true;

        if (data != null)
        {
            var configurationsTable = Payload.Project.ConfigurationManager.ConfigurationRowNames;
            if (configurationsTable is IEnumerable enumerableConfigurations)
            {
                var runConfiguration = data.RunConfiguration;
                var count = 0;

                foreach (var configuration in enumerableConfigurations)
                {
                    if (configuration is not string configName || configName != runConfiguration)
                    {
                        ++count;
                        continue;
                    }

                    ProjectRunCombo.SelectedIndex = count;
                    break;
                }
            }

            var validationRule = new ProjectTokenValidationRule();
            var validationResult = validationRule.Validate(data.ProjectToken, CultureInfo.CurrentCulture);

            SaveButton.IsEnabled = validationResult.IsValid;
        }

        if (ProjectRunCombo.SelectedIndex == -1)
        {
            // ProjectRunCombo.SelectedItem ??= Payload.Project.ConfigurationManager.ActiveConfiguration;
            if (Payload.Project.ConfigurationManager is { Count: > 0 })
            {
                ProjectRunCombo.SelectedIndex = Payload.Project.ConfigurationManager.Count - 1;
            }

            else
            {
                ProjectRunCombo.SelectedIndex = 0;
            }
        }

        // var runConfigurations = Payload.Project.ConfigurationManager.Cast<Configuration>().Select(x => x.ConfigurationName);
        // var itemsSource = runConfigurations as string[] ?? runConfigurations.ToArray();
        // 
        // ProjectRunCombo.ItemsSource = itemsSource;
        // ProjectRunCombo.SelectedIndex = itemsSource.ToList().IndexOf(data.RunConfiguration);
    }

    private void SaveButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { IsInitialized: true } control) return;
        if (!control.IsMouseOver) return;
        if (Payload == null) return;

        ThreadHelper.ThrowIfNotOnUIThread();

        LocalStorage = new SecureLocalStorage(new CustomLocalStorageConfig(null, Globals.ShieldLocalStorageName)
            .WithDefaultKeyBuilder());

        var data = LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid()) ??
                   new ShieldConfiguration();

        dynamic runConfigurationSelected = ProjectRunCombo.SelectedItem;

        data.Name = $"{ProjectNameBox.Text}";
        data.Preset ??= EPresetType.Optimized.ToFriendlyString();
        data.ProjectToken = ProjectTokenBox.Text;
        data.ProtectionSecret = SecretBox.Password;
        data.Enabled = StatusToggle.IsChecked ?? false;
        data.RunConfiguration = runConfigurationSelected?.ConfigurationName ?? "Release";

        LocalStorage.Set(Payload.Project.UniqueName.ToUuid(), data);

        // _ = FileManager.WriteJsonShieldConfiguration(Payload.FolderName,
        _ = FileManager.WriteJsonShieldConfiguration(FileManager.GetParentDirFromFile(Payload.Project.FullName),
            JsonHelper.Stringify(LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid())));

        ViewModelBase.ProjectChangedHandler.Invoke(Payload);
        // MessageBox.Show(saved ? $"Saving for {Payload.Name}" : $"Failed to save for {Payload.Name}");
    }

    private void ProjectTokenBoxOnChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox control) return;
        var password = control.Text;

        var validationRule = new ProjectTokenValidationRule();
        var validationResult = validationRule.Validate(password, CultureInfo.CurrentCulture);

        SaveButton.IsEnabled = validationResult.IsValid;
    }

    private void OnFree(object sender, RoutedEventArgs e)
    {
        ViewModelBase.ProjectChangedHandler -= OnProjectChanged;
    }

    #region Commands

    public ProjectViewModel Payload
    {
        get => (ProjectViewModel)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    public static readonly DependencyProperty PayloadProperty = DependencyProperty.Register(
        nameof(Payload),
        typeof(ProjectViewModel),
        typeof(SettingsControl),
        new PropertyMetadata(null));

    #endregion
}