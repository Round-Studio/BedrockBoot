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
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Core.Global;
using BedrockBoot.Localization;
using BedrockBoot.Localization.Avalonia;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalLanguage : ISettingPage
{
    public UniversalLanguage()
    {
        InitializeComponent();

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
                ItemName = I18nManager.Instance["Setting.Language.Title"]
            }
        };

        LanguageSelectBox.SelectedIndex =
            I18nManager.LanguageDict.Keys.ToList().FindIndex(x => x == GlobalModel.Config.Data.Language);

        // 缺翻译显示策略：选项内容需要按当前语言取，故在代码里填充。
        // 枚举值直接当下拉框索引用（顺序固定，见 MissingKeyStyle）。
        MissingKeyStyleBox.Items.Clear();
        foreach (var style in Enum.GetValues<MissingKeyStyle>())
            MissingKeyStyleBox.Items.Add(new ComboBoxItem
            {
                Content = I18nManager.Instance["Setting.Language.MissingKey." + style]
            });
        MissingKeyStyleBox.SelectedIndex = (int)GlobalModel.Config.Data.MissingKeyStyle;

        IsEdit = true;
    }

    private void MissingKeyStyleBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit || MissingKeyStyleBox.SelectedIndex < 0)
            return;

        var style = (MissingKeyStyle)MissingKeyStyleBox.SelectedIndex;
        if (GlobalModel.Config.Data.MissingKeyStyle == style)
            return;

        GlobalModel.Config.Data.MissingKeyStyle = style;
        GlobalModel.Config.Save();

        // 换策略必须重建键表，否则界面不会变（仅换 Options 实例不会触发重建）。
        LanguageService.Current.SetMissingKeyStyle(style);
    }

    private void LanguageSelectBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.Language = I18nManager.LanguageDict.Keys.ToList()[LanguageSelectBox.SelectedIndex];
            GlobalModel.Config.Save();
            
            I18nManager.Instance.SystemLanguage(GlobalModel.Config.Data.Language);
        }
    }

    private async void LocalFileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storageProvider)
            return;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = I18nManager.Instance["Setting.Language.DebugTool.Picker.Title"],
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(I18nManager.Instance["Setting.Language.DebugTool.Picker.LanguageFiles"])
                {
                    Patterns = new[] { "*.axaml", "*.xaml", "*.xml" }
                },
                new FilePickerFileType(I18nManager.Instance["Setting.Language.DebugTool.Picker.AllFiles"])
                {
                    Patterns = new[] { "*" }
                }
            }
        });

        if (files.Count == 0)
            return;

        var file = files[0];
        var path = file.TryGetLocalPath();
        if (path is null)
            return;

        I18nManager.Instance.DebugLanguage(path);
    }

    private void OpenPRBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder.Open("https://github.com/Round-Studio/BedrockBoot/pulls");
    }
}