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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Standard.Entity.Info.Download;
using BedrockBoot.Standard.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Interface.Download;
using BedrockBoot.Service.Download;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using BedrockBoot.Views.Control.Items.Download;
using BedrockBoot.Views.Helper;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultFiles : UserControl
{
    private readonly IDownloadResult _service;

    public ResultFiles()
    {
        InitializeComponent();
    }

    public ResultFiles(IDownloadResult service) : this()
    {
        _service = service;
        _ = UpdateUi();
    }

    private List<ResourceFileInfo> _files = new();

    // 分组键去重保序：接口按最新在前返回，首现顺序即「最新分组在前」
    private List<string> _groupKeys = new();
    private int _selectedGroup = -1;

    private async Task UpdateUi()
    {
        try
        {
            _files = await _service.GetFiles() ?? new List<ResourceFileInfo>();
            _groupKeys = _files
                .Select(f => f.GroupKey ?? f.VersionGroup)
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct()
                .ToList();

            RenderPills();
            RenderFiles();
        }
        catch (System.Exception ex) when (_service is PluginDownloadResult or LeviLaminaDownloadResult or DllModDownloadResult)
        {
            await GitHubErrorDialog.ShowAsync(ex);
        }
    }

    private void RenderPills()
    {
        PillsPanel.Children.Clear();
        PillsPanel.IsVisible = _groupKeys.Count > 1;
        if (!PillsPanel.IsVisible)
        {
            _selectedGroup = -1;
            return;
        }

        PillsPanel.Children.Add(MakePill(I18nManager.Instance["Download.Result.Group.All"], -1));
        for (var i = 0; i < _groupKeys.Count; i++)
            PillsPanel.Children.Add(MakePill(PillLabel(_groupKeys[i]), i));
    }

    // 胶囊文案优先取分组副标题（如 LeviLamina 26.60.9），缺失时退回分组标题
    private string PillLabel(string groupKey)
    {
        var first = _files.FirstOrDefault(f => (f.GroupKey ?? f.VersionGroup) == groupKey);
        return first?.GroupSubtitle ?? first?.GroupTitle ?? first?.VersionGroup ?? groupKey;
    }

    private Button MakePill(string label, int index)
    {
        var pill = new Button
        {
            Classes = { "NoBorder" },
            Padding = new Thickness(16, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Content = label
        };

        if (_selectedGroup == index &&
            Application.Current?.TryFindResource("AccentBackgroundBrush", Application.Current.ActualThemeVariant, out var accent) == true &&
            accent is Brush accentBrush)
            pill.Background = accentBrush;

        pill.Click += (s, e) =>
        {
            _selectedGroup = index;
            RenderPills();
            RenderFiles();
        };

        return pill;
    }

    private void RenderFiles()
    {
        FilesList.Children.Clear();

        var filtered = _selectedGroup >= 0 && _selectedGroup < _groupKeys.Count
            ? _files.Where(f => (f.GroupKey ?? f.VersionGroup) == _groupKeys[_selectedGroup]).ToList()
            : _files;

        foreach (var group in filtered.GroupBy(f => f.GroupKey ?? f.VersionGroup))
        {
            // 无分组信息的文件平铺
            if (string.IsNullOrEmpty(group.Key))
            {
                foreach (var file in group)
                    FilesList.Children.Add(new ResourceFileItem(file));
                continue;
            }

            var files = group.ToList();
            var first = files[0];
            var title = first.GroupTitle ?? first.VersionGroup;
            var subtitle = first.GroupSubtitle;

            var rows = new StackPanel { Spacing = 0 };
            foreach (var file in files)
                rows.Children.Add(new ResourceFileItem(file));

            var expander = new SettingExpander
            {
                IsExpanded = !string.Equals(group.Key, "unknown", StringComparison.Ordinal),
                Content = rows,
                Glyph = "\uE7FC"
            };

            expander.Header = new TextBlock
            {
                Text = title,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontWeight = FontWeight.Bold
            };
            if (!string.IsNullOrEmpty(subtitle))
                expander.Description = new TextBlock { Text = subtitle, FontSize = 11, Opacity = 0.8 };
            expander.RightContent = new TextBlock
            {
                Text = string.Format(I18nManager.Instance["Download.Result.VersionGroup.Count"], files.Count)
            };

            FilesList.Children.Add(expander);
        }
    }
}
