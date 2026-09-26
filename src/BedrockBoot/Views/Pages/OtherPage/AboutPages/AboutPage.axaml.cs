/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.OtherPage;

public partial class AboutPage : ISettingPage
{
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

    private static I18nManager i18n => I18nManager.Instance;

    /// <summary>
    ///     导航至开源组件页面
    /// </summary>
    private void OpenSourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutOpenSource());
    }

    /// <summary>
    ///     导航至贡献者页面
    /// </summary>
    private void ContributorsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new AboutContributor());
    }

    private void VersionCard_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalSoftwareUpdate());
    }
}