using System.Windows;
using ShieldVSExtension.Common.Helpers;
using ShieldVSExtension.Common.Models;
using ShieldVSExtension.Common;
using ShieldVSExtension.Storage;
using ShieldVSExtension.ViewModels;
using ShieldVSExtension.Common.Extensions;
using System.Diagnostics;

namespace ShieldVSExtension.UI.Views.Presets;

/// <summary>
/// Interaction logic for MaximumControl.xaml
/// </summary>
public partial class MaximumControl
{
    public SecureLocalStorage LocalStorage { get; set; }
    public ProjectViewModel Payload { get; set; }

    public MaximumControl()
    {
        InitializeComponent();

        ViewModelBase.ProjectChangedHandler += OnRefresh;
        ViewModelBase.TabSelectedHandler += OnSelected;

        Unloaded += OnFree;
    }

    private void OnRefresh(ProjectViewModel payload)
    {
        if (payload == null) return;

        Payload = payload;
    }

    private void OnSelected(EPresetType preset)
    {
        if (preset != EPresetType.Maximum || Payload == null) return;

        SaveConfiguration();
    }

    private void SaveConfiguration()
    {
        try
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            LocalStorage = new SecureLocalStorage(new CustomLocalStorageConfig(null, Globals.ShieldLocalStorageName)
                .WithDefaultKeyBuilder());

            var data = LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid()) ??
                       new ShieldConfiguration();

            if (string.IsNullOrWhiteSpace(data.ProjectToken)) return;

            var preset = EPresetType.Maximum.ToFriendlyString();
            if (data.Preset == preset) return;

            data.Preset = preset;
            LocalStorage.Set(Payload.Project.UniqueName.ToUuid(), data);

            // FileManager.WriteJsonShieldConfiguration(Payload.FolderName,
            FileManager.WriteJsonShieldConfiguration(FileManager.GetParentDirFromFile(Payload.Project.FullName),
                JsonHelper.Stringify(LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid())));
        }
        catch (System.Exception ex)
        {
            // LocalStorage?.Remove(Payload.Project?.UniqueName);
            Debug.WriteLine(ex.Message);
        }
    }

    private void OnFree(object sender, RoutedEventArgs e)
    {
        ViewModelBase.ProjectChangedHandler -= OnRefresh;
        // ViewModelBase.TabSelectedHandler -= OnSelected;
    }
}