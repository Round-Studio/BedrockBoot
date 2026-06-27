using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
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
        IsolationPriority.SelectedIndex = (int)GlobalModel.Config.Data.IsolationPriority;
        CatalogStrategy.SelectedIndex = ((int)GlobalModel.Config.Data.CatalogStrategy) - 1;

#if RELEASE && LINUX
        SeniorPanel.IsVisible = Models.Global.GlobalModel.FunctionOption.IsEnableGameProtonManager;
#endif

#if LINUX
        IsolationCard.IsVisible = false;
        MouseLockBtn.IsVisible = false;
#endif

#if LINUX && DEBUG
        SeniorPanel.IsVisible = true;
#endif

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

     private void MouseLockBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new MouseLock());
    }

    private void ProtonBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GameProton());
    }

    private void IsolationPriority_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsolationPriority = (IsolationModelEnum)IsolationPriority.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void CatalogStrategy_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.CatalogStrategy = (CatalogStrategyEnum)(CatalogStrategy.SelectedIndex + 1);
            GlobalModel.Config.Save();
        }
    }
}