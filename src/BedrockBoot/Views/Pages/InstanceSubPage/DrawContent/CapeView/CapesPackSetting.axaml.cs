using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesPackSetting : UserControl
{
    private readonly VersionConfig _versionInfo;
    private readonly string _folderName;

    public CapesPackSetting()
    {
        InitializeComponent();
    }

    public CapesPackSetting(VersionConfig versionInfo, string folderName) : this()
    {
        _versionInfo = versionInfo;
        _folderName = folderName;
        UpdateUI();
    }

    public Action? BackAction { get; set; }

    public void UpdateUI()
    {
        var pack = CapesPackManager.LoadPacks(_versionInfo.VersionPath)
            .Find(p => p.FolderName == _folderName);
        PackNameLabel.Text = pack?.PackName ?? _folderName;
        NavigationFrame.NavigateTo(new CapesFileView(_versionInfo, _folderName));
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        BackAction?.Invoke();
    }

    private void NavBar_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavBar?.SelectedItem is not ListBoxItem item) return;
        if (_versionInfo == null) return;
        var tag = item.Tag?.ToString();
        object? page = tag switch
        {
            "Capes" => new CapesFileView(_versionInfo, _folderName),
            "Setting" => new CapesPackInfoPage(_versionInfo, _folderName),
            _ => null
        };

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}