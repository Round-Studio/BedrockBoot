using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Language;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingUniversal : ISettingPage
{
    

    public SettingUniversal()
    {
        InitializeComponent();
        
        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                // 使用国际化 Key，当语言切换时，Breadcrumb 通常需要重新加载或使用绑定
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"]
            }
        };

        // 初始化 UI 状态
        TaskBarJumpItem.IsChecked = BedrockBoot.Core.Global.GlobalModel.Config.Data.IsTaskBarJumpItem;
        GatInfo.IsChecked = BedrockBoot.Core.Global.GlobalModel.Config.Data.GatherInfo;
        LanguageChoose.SelectedIndex = (int)BedrockBoot.Core.Global.GlobalModel.Config.Data.Language;
        LaunchBehaviorChoose.SelectedIndex = (int)BedrockBoot.Core.Global.GlobalModel.Config.Data.LaunchBehavior;
        
        IsEdit = true;
    }

    private void SoftwareUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalSoftwareUpdate());
    }

    private void DebugBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalDebug());
    }

    private void TaskBarJumpItem_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.IsTaskBarJumpItem = TaskBarJumpItem.IsChecked ?? false;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();

            JumpListManager.ConfigureJumpList();
        }
    }

    private void LanguageChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            // 更新配置中的语言枚举
            var selectedLanguage = (LanguageEnum)LanguageChoose.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Data.Language = selectedLanguage;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();

            // 执行语言切换核心逻辑
            I18nManager.Instance.SystemLanguage(selectedLanguage);
            
            // 提示：由于 BreadcrumbItem 是在构造函数赋值的，
            // 如果需要立即更新面包屑文字，可以在此处重新赋值：
            BreadcrumbItem[0].ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"];
        }
    }

    private void GatInfo_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.GatherInfo = (bool)GatInfo.IsChecked!;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
    }

    private void LaunchBehaviorChoose_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            BedrockBoot.Core.Global.GlobalModel.Config.Data.LaunchBehavior = (LaunchBehaviorEnum)LaunchBehaviorChoose.SelectedIndex;
            BedrockBoot.Core.Global.GlobalModel.Config.Save();
        }
    }
}