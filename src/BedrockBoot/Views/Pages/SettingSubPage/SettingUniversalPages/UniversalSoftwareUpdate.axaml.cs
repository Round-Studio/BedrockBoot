using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalSoftwareUpdate : ISetting
{
    public UniversalSoftwareUpdate()
    {
        InitializeComponent();
        IsAutoCheckUpdate.IsChecked = GlobalModel.Config.Data.IsAutoCheckUpdate;
        UpdateTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.UpdateType;
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "通用",
                ItemClickAction = info =>
                    MainSettingPage.NavigationFrame.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = "软件更新"
            }
        });

        IsEdit = true;
    }

    private void IsAutoCheckUpdate_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsAutoCheckUpdate = (bool)IsAutoCheckUpdate.IsChecked;
            GlobalModel.Config.Save();
        }
    }

    private void UpdateTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.UpdateType = (UpdateType)UpdateTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }
}