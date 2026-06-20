using System.Collections.Generic;
using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

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
        UpdateUI();

#if RELEASE
        // 根据功能开关控制启用状态
        SetBackground.IsEnabled = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableSettingBackground;
        SetColor.IsEnabled = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableSettingColor;
        HomePanel.IsVisible = BedrockBoot.Models.Global.GlobalModel.FunctionOption.IsEnableSettingPersonalizationHome;
#endif
    }

    public void UpdateUI()
    {
        IsEdit = false;

        IsUseSystemWindow.IsChecked = GlobalModel.Config.Data.IsUseSystemWindow;
        IsUseThemePack.IsChecked = GlobalModel.Config.Data.StyleConfig.IsUseThemePack;

        SetBackground.IsVisible = !GlobalModel.Config.Data.StyleConfig.IsUseThemePack;
        SetColor.IsVisible = !GlobalModel.Config.Data.StyleConfig.IsUseThemePack;
        SaveThemePack.IsVisible = !GlobalModel.Config.Data.StyleConfig.IsUseThemePack &&
                                  GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image &&
                                  !string.IsNullOrEmpty(GlobalModel.Config.Data.StyleConfig.BackgroundImage);
        ThemePackManager.IsVisible = GlobalModel.Config.Data.StyleConfig.IsUseThemePack;

        IsEdit = true;
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

            Models.Global.GlobalModel.MainWindow.UpdateWindowBorder();
        }
    }

    private void SetAudio_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new PersonalizationAudio());
    }

    private void IsUseThemePack_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.IsUseThemePack = IsUseThemePack.IsChecked ?? false;
            GlobalModel.Config.Save();
            UpdateUI();
        }
    }

    private void SaveThemePack_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogMakeThemePackContent();
        DialogHost.Show(new ()
        {
            Content = dialog,
            Title = "导出主题包",
            CloseButtonText = "导出",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var manifest = dialog.Manifest;
                var maker = new ThemePackMaker(manifest);
                
                maker.StartMake("E://test.rskin");
            }
        });
    }
}