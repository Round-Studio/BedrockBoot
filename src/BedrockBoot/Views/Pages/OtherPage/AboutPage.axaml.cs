using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutPage : ISettingPage
{
    private static I18nManager i18n => I18nManager.Instance;

    public AboutPage()
    {
        InitializeComponent();

        // 设置面包屑导航
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new() { ItemName = i18n["AboutPage.Title"] }
        };

        // 设置版本卡片描述
        VersionCard.Description = GlobalModel.BodyVersion;
        
        // 动态显示框架驱动信息
        var avaloniaVersion = typeof(AppBuilder).Assembly.GetName().Version;
        PowerByTextBlock.Text = $"Power By: Avalonia {avaloniaVersion}";
    }

    /// <summary>
    /// 处理检查更新按钮点击事件
    /// </summary>
    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 1. 进入加载状态
        CheckUpdateBtn.IsEnabled = false;
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 20,
            Height = 20,
            Foreground = Brushes.White, // 假设背景深色，可根据实际主题调整
            Background = Brushes.Transparent
        };

        try
        {
            // 2. 调用全局更新逻辑
            // 假设 MainPage.Update(true) 内部会处理弹窗提醒更新结果
            await MainPage.Update(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
        finally
        {
            // 3. 恢复按钮状态
            CheckUpdateBtn.IsEnabled = true;
            CheckUpdateBtn.Content = i18n["AboutPage.Update.Action"];
        }
    }

    /// <summary>
    /// 导航至开源组件页面
    /// </summary>
    private void OpenSourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutOpenSource());
    }

    /// <summary>
    /// 导航至贡献者页面
    /// </summary>
    private void ContributorsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutContributor());
    }
}