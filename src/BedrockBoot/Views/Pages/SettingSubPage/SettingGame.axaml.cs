using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Models;
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
        IsOpenGameLayering.IsChecked = GlobalModel.Config.Data.IsOpenGameLayering;
        IsUseMultipleUsers.IsChecked = GlobalModel.Config.Data.IsUseMultipleUsers;
        IsUseNeoLaunchBox.IsVisible = false;
        ProtonBtn.IsVisible = false;

#if LINUX
        IsolationCard.IsVisible = false;
        IsUseMultipleUsersCard.IsVisible = false;
        HelperPanel.IsVisible = false;
        MouseLockBtn.IsVisible = false;
        ProtonBtn.IsVisible = !GlobalModel.Config.Data.IsUseNeoLaunch;
        IsUseNeoLaunchBox.IsVisible = true;
        IsUseNeoLaunchToggleSwitch.IsChecked = GlobalModel.Config.Data.IsUseNeoLaunch;
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

    private void PublicSyncBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GamePublicSync());
    }

    private void LaunchCommandBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GameLaunchCommand());
    }

    private void IsUseNeoLaunchToggleSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsUseNeoLaunch = (bool)IsUseNeoLaunchToggleSwitch.IsChecked!;
            GlobalModel.Config.Save();
            
            Models.Global.GlobalModel.MainWindow.SetReboot();

#if LINUX
            CoreInit.UpdateUseNeoLaunch(GlobalModel.Config.Data.IsUseNeoLaunch);
            ProtonBtn.IsVisible = !GlobalModel.Config.Data.IsUseNeoLaunch;
#endif
        }
    }

    private void IsOpenGameLayering_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsOpenGameLayering = (bool)IsOpenGameLayering.IsChecked!;
            GlobalModel.Config.Save();
        }
    }

    private void IsUseMultipleUsers_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsUseMultipleUsers = (bool)IsUseMultipleUsers.IsChecked!;
            GlobalModel.Config.Save();
            
            Models.Global.GlobalModel.MainWindow.SetReboot();
        }
    }
}