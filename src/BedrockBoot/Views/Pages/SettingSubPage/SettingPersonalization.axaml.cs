using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingPersonalization : ISettingPage
{
    public SettingPersonalization()
    {
        InitializeComponent();
        
        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Personalization.Breadcrumb.Root"]
            }
        };

        IsUseSystemWindow.IsChecked = GlobalModel.Config.Data.IsUseSystemWindow;

        IsEdit = true;

#if RELEASE
        // 根据功能开关控制启用状态
        SetBackground.IsEnabled = GlobalModel.FunctionOption.IsEnableSettingBackground;
        SetColor.IsEnabled = GlobalModel.FunctionOption.IsEnableSettingColor;
        HomePanel.IsVisible = GlobalModel.FunctionOption.IsEnableSettingPersonalizationHome;
#endif
    }

    private void SetColor_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PersonalizationColor());
    }

    private void SetBackground_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PersonalizationBackground());
    }

    private void SetHome_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PersonalizationHome());
    }

    private void IsUseSystemWindow_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsUseSystemWindow =
                (bool)IsUseSystemWindow.IsChecked!;
            GlobalModel.Config.Save();
            
            GlobalModel.MainWindow.UpdateWindowBorder();
        }
    }
}