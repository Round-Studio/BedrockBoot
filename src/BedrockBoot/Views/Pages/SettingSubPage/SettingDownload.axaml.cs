using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingDownload : UserControl
{
    public bool IsEdit = false;
    public SettingDownload()
    {
        InitializeComponent();
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new ()
            {
                ItemName = "下载"
            }
        });

        IsAutoCacheGamePack.IsChecked = GlobalModel.Config.Data.IsAutoCacheGamePack;
    }

    private void IsAutoCacheGamePack_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
             GlobalModel.Config.Data.IsAutoCacheGamePack = (bool)IsAutoCacheGamePack.IsChecked;
             GlobalModel.Config.Save();
        }
    }
}