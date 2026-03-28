using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Interface;
using BedrockBoot.LevelNbt;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsRoot : ISetting
{
    private ArchiveInfo _info;
    private bool _isInternalUpdating = false;
    public Action? BackAction { get; set; }

    public LevelSettingsRoot() => InitializeComponent();

    public LevelSettingsRoot(ArchiveInfo info) : this()
    {
        _info = info;
        UpdateUI();
    }

    private void UpdateUI()
    {
        NavigationFrame.NavigateTo(new LevelSettingsEditor(_info));
        LevelNameLabel.Text = _info.LevelWorldData.LevelName;
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e) => BackAction?.Invoke();

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var tag = ((ListBoxItem)NavBar?.SelectedItem!).Tag?.ToString();
        Object? page = null;
        if (tag != null)
        {
            switch (tag)
            {
                case "Backup":
                    page = new LevelSettingsBackup(_info);
                    break;
                case "Setting":
                    page = new LevelSettingsEditor(_info);
                    break;
            }
        }

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}