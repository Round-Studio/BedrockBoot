using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Interface;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsRoot : ISetting
{
    private readonly ArchiveInfo _info;
    private bool _isInternalUpdating = false;

    public LevelSettingsRoot()
    {
        InitializeComponent();
    }

    public LevelSettingsRoot(ArchiveInfo info) : this()
    {
        _info = info;
        UpdateUI();
    }

    public Action? BackAction { get; set; }

    private void UpdateUI()
    {
        NavigationFrame.NavigateTo(new LevelSettingsEditor(_info));
        LevelNameLabel.Text = _info.LevelWorldData.LevelName;
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        BackAction?.Invoke();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var tag = ((ListBoxItem)NavBar?.SelectedItem!).Tag?.ToString();
        object? page = null;
        if (tag != null)
            switch (tag)
            {
                case "Backup":
                    page = new LevelSettingsBackup(_info);
                    break;
                case "Setting":
                    page = new LevelSettingsEditor(_info);
                    break;
                case "Controls":
                    page = new LevelSettingsControls(_info);
                    break;
            }

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}