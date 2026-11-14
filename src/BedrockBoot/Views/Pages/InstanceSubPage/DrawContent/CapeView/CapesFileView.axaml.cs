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
using System.IO;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesFileView : UserControl
{
    private readonly CapesPackUnit _unit;

    public CapesFileView()
    {
        InitializeComponent();
    }
    public CapesFileView(CapesPackUnit unit):this()
    {
        _unit = unit;
        UpdateContent();
    }

    private void UpdateContent()
    {
        CapesBox.Children.Clear();

        var capes = _unit.LoadCapeFile();
        NullBox.IsVisible = capes.Count == 0;

        foreach (var stableKey in capes)
        {
            // CapesBox.Children.Add();
        }
    }

    private async void AddCapeBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择披风纹理",
            AllowMultiple = true,
            FileTypeFilter = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
        });
        if (files is not { Count: >= 1 }) return;

        var added = 0;
        var errors = new List<string>();
        foreach (var file in files)
        {
            /*var error = CapesPackManager.AddCape(_versionInfo.VersionPath, _folderName, file.Path.LocalPath);
            if (error is null)
            {
                added++;
            }
            else
            {
                errors.Add($"{file.Name}: {error}");
            }*/
        }

        UpdateContent();

        if (errors.Count > 0)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = "添加披风失败",
                Message = string.Join("\n", errors),
                NoticeType = NoticeType.Warning
            });
        }
        else if (added > 0)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = "成功",
                Message = string.Format("已添加 {0} 件披风", added),
                NoticeType = NoticeType.Info
            });
        }
    }
}
