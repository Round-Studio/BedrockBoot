using System;
using System.Collections.Generic;
using Avalonia;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutOpenSource : ISettingPage
{
    public AboutOpenSource()
    {
        InitializeComponent();

        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = i18n["AboutPage.Title"], // "关于我们"
                ItemClickAction = _ => MainSettingPage.NavigateTo(new AboutPage())
            },
            new()
            {
                ItemName = i18n["AboutPage.OpenSource.Title"] // "第三方组件库"
            }
        };

        InitializeFrameworkVersion();
    }

    private static I18nManager i18n => I18nManager.Instance;

    /// <summary>
    ///     获取并显示核心框架版本
    /// </summary>
    private void InitializeFrameworkVersion()
    {
        try
        {
            var type = typeof(AppBuilder);
            var assembly = type.Assembly;
            var version = assembly.GetName().Version;

            // 使用国际化前缀，例如 "版本 11.0.0" 或 "Version 11.0.0"
            AvaloniaVersion.Text = $"{i18n["AboutPage.OpenSource.VersionPrefix"]} {version}";
        }
        catch (Exception)
        {
            AvaloniaVersion.Text = "Unknown";
        }
    }
}