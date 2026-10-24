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

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.DevelopPage.Plugin;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalProjectManager : ISettingPage
{
    public UniversalProjectManager()
    {
        InitializeComponent();
        
        MainFrame.NavigateTo(new ProjectList());

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Debug.Title"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new UniversalDebug())
            },
            new()
            {
                ItemName = "项目管理"
            }
        };
    }

    private void CreatProject_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogCreatePluginProjectContent();
        DialogHost.Show(new()
        {
            Title = "创建插件项目",
            Content = dialog,
            CloseButtonText = "创建",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var packConf = dialog.PackConfig;
                DialogHost.Show(new()
                {
                    Content = new DialogCreatePluginProjectLoadingContent(packConf),
                    Title = "创建项目"
                });
            }
        });
    }
}