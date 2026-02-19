using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingGame : ISettingPage
{
    public SettingGame()
    {
        InitializeComponent();
        
        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"]
            }
        };

        // 从配置中还原隔离模式索引
        IsolationTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.IsolationModel;

        IsEdit = true;
    }

    private void IsolationTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            // 更新配置中的隔离模式
            GlobalModel.Config.Data.IsolationModel = (IsolationType)IsolationTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void GameFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 导航至游戏目录管理子页面
        MainSettingPage.NavigateTo(new GameFolders());
    }
}