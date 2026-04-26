using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Helper;
using BedrockBoot.Proton.Entry.Info;

namespace BedrockBoot.Views.Control.Items.Proton;

public partial class InstalledProtonItem : UserControl
{
    private readonly ProtonInfo _info;
    private readonly Action _updateUi;

    public InstalledProtonItem()
    {
        InitializeComponent();
    }

    public InstalledProtonItem(ProtonInfo info, Action updateUi) : this()
    {
        _info = info;
        _updateUi = updateUi;
        UpdateUI();
    }

    public void UpdateUI()
    {
        Card.Description = $"{_info.Version}, 来自 {_info.Branch}";
        ProtonName.Text = _info.Name;
        IsDefault.IsVisible = _info.IsDefault;
        DeleteProtonBtn.IsVisible = !_info.IsDefault;
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(_info.InstallPath);
    }
}