﻿using System.Windows;
using ShieldVSExtension.Common.Helpers;
using ShieldVSExtension.Common.Models;
using ShieldVSExtension.Common;
using ShieldVSExtension.Storage;
using ShieldVSExtension.ViewModels;
using ShieldVSExtension.Common.Extensions;

namespace ShieldVSExtension.UI.Views.Presets;

/// <summary>
/// Interaction logic for BalanceControl.xaml
/// </summary>
public partial class BalanceControl
{
    public SecureLocalStorage LocalStorage { get; set; }
    public ProjectViewModel Payload { get; set; }

    public BalanceControl()
    {
        InitializeComponent();
        ViewModelBase.ProjectChangedHandler += OnProjectChanged;
        ViewModelBase.TabSelectedHandler += OnSelected;

        Unloaded += OnFree;
    }

    private void OnProjectChanged(ProjectViewModel payload)
    {
        if (payload == null) return;

        Payload = payload;
    }

    private void OnSelected(EPresetType preset)
    {
        if (preset != EPresetType.Balance || Payload == null) return;

        SaveConfiguration();
    }

    private void SaveConfiguration()
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

        LocalStorage = new SecureLocalStorage(new CustomLocalStorageConfig(null, Globals.ShieldLocalStorageName)
            .WithDefaultKeyBuilder());

        // try
        // {
        var data = LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid()) ?? new ShieldConfiguration();
        if (string.IsNullOrWhiteSpace(data.ProjectToken)) return;

        var balance = EPresetType.Balance.ToFriendlyString();
        if (data.Preset == balance) return;

        data.Preset = balance;
        LocalStorage.Set(Payload.Project.UniqueName.ToUuid(), data);

        FileManager.WriteJsonShieldConfiguration(Payload.FolderName,
            JsonHelper.Stringify(LocalStorage.Get<ShieldConfiguration>(Payload.Project.UniqueName.ToUuid())));
        // }
        // catch (System.Exception ex)
        // {
        //     LocalStorage.Remove(Payload.Project.UniqueName);
        //     MessageBox.Show(ex.Message);
        // }
    }

    private void OnFree(object sender, RoutedEventArgs e)
    {
        ViewModelBase.ProjectChangedHandler -= OnProjectChanged;
        ViewModelBase.TabSelectedHandler -= OnSelected;
    }
}