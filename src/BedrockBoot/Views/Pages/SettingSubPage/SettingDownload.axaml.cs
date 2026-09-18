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
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.SettingSubPage.SettingDownloadPages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;
using OnePointUI.Avalonia.Base.Entry;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingDownload : ISettingPage
{
    public SettingDownload()
    {
        InitializeComponent();

        // 面包屑导航国际化
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Download.Breadcrumb.Root"]
            }
        };

        // 加载下载分片数配置
        ChunkCountSlider.Value = GlobalModel.Config.Data.DownloadChunkCount;

        // 动态加载版本下载源 (保持数据源原名)
        SourceBox.Items.Clear();
        foreach (var s in SourceList.VersionDataSources) SourceBox.Items.Add(new ComboBoxItem { Content = s.Key });
        SourceBox.SelectedIndex = GlobalModel.Config.Data.VersionSourceIndex;

        // 动态加载 CurseForge 下载源
        CurseForgeSourceBox.Items.Clear();
        foreach (var s in SourceList.CurseForgeSource)
            CurseForgeSourceBox.Items.Add(new ComboBoxItem { Content = s.Key });
        CurseForgeSourceBox.SelectedIndex = GlobalModel.Config.Data.CurseForgeSourceIndex;

        EnableFuzzySearch.IsChecked = GlobalModel.Config.Data.IsEnableFuzzySearch;

        IsEdit = true;
    }

    private void GameFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new GameFolders());
    }

    private void SoftwareUpdate_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new UniversalSoftwareUpdate());
    }

    private void NetworkTestBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainSettingPage.NavigateTo(new NetworkTest());
    }

    private void ChunkCountSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (IsEdit)
        {
            var newValue = (int)ChunkCountSlider.Value;
            if (newValue != GlobalModel.Config.Data.DownloadChunkCount)
            {
                GlobalModel.Config.Data.DownloadChunkCount = newValue;
                GlobalModel.Config.Save();
            }
        }
    }

    private void SourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.VersionSourceIndex = SourceBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void CurseForgeSourceBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.CurseForgeSourceIndex = CurseForgeSourceBox.SelectedIndex;
            GlobalModel.Config.Save();
        }
    }

    private void EnableFuzzySearch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.IsEnableFuzzySearch = EnableFuzzySearch.IsChecked ?? false;
            GlobalModel.Config.Save();
        }
    }
}