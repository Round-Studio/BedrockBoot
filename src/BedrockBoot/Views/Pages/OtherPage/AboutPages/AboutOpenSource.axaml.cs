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