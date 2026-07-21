using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GamePublicSync : ISettingPage
{
    public GamePublicSync()
    {
        InitializeComponent();
        
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "全局配置"
            }
        };
    }

    private void ResetBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.Config.Data.PublicOptionsConfig = null;
        GlobalModel.Config.Save();
    }
}