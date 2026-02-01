using System.Collections.Generic;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalDebug : ISetting
{
    public UniversalDebug()
    {
        InitializeComponent();
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
                ItemName = "调试模式"
            }
        });

        IsConsoleModel.IsChecked = GlobalModel.Config.Data.IsConsole;
        IsEdit = true;
    }

    private void CheckBox_Change(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsConsole = (bool)IsConsoleModel.IsChecked;

            GlobalModel.Config.Save();
        }
    }
}