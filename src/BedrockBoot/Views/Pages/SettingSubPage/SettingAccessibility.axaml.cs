using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingAccessibility : ISettingPage
{
    public SettingAccessibility()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "辅助功能"
            }
        };

        UIZoom.SelectedIndex = (int)GlobalModel.Config.Data.UIZoom;

        IsEdit = true;
    }

    private void SaveBackupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GameBackup());
    }

    private void MouseLockBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new MouseLock());
    }

    private void UIZoom_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            IsEdit = false;
            GlobalModel.Config.Data.UIZoom = (UIZoom)UIZoom.SelectedIndex;
            GlobalModel.Config.Save();

            Models.Global.GlobalModel.MainWindow.UpdateZoom();
            IsEdit = true;
        }
    }
}