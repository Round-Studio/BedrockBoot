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
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalSoftwareUpdate : ISettingPage
{
    public bool IsEdit;

    public UniversalSoftwareUpdate()
    {
        InitializeComponent();
        IsAutoCheckUpdate.IsChecked = GlobalModel.Config.Data.IsAutoCheckUpdate;
        UpdateTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.UpdateType;

        // 版本描述：直接显示版本号，或拼接“当前版本：”
        VersionCard.Description = Models.Global.GlobalModel.BodyVersion;

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
                ItemName = I18nManager.Instance["Setting.Universal.SoftwareUpdate.Title"]
            }
        };

        // 联机依赖组件仅 Windows 提供，Linux 下隐藏入口
#if LINUX
        DependencyUpdateBtn.IsVisible = false;
#endif

        IsEdit = true;
    }

    private void DependencyUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalDependencyUpdate());
    }

    private void IsAutoCheckUpdate_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsAutoCheckUpdate = IsAutoCheckUpdate.IsChecked ?? false;
            GlobalModel.Config.Save();
        }
    }

    private void UpdateTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.UpdateType = (UpdateType)UpdateTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;

        // 替换为等待状态
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };

        await MainPage.Update(true);

        CheckUpdateBtn.IsEnabled = true;
        // 恢复按钮文本
        CheckUpdateBtn.Content = I18nManager.Instance["Setting.Universal.SoftwareUpdate.CheckUpdateAction"];
    }
}