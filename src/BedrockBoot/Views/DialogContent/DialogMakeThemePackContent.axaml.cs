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
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Pack.Theme;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeThemePackContent : UserControl
{
    public DialogMakeThemePackContent()
    {
        InitializeComponent();
    }

    public ThemePackManifest Manifest => new()
    {
        PackAuthor = string.IsNullOrEmpty(ThemePackAuthor.Text) ? string.Empty : ThemePackAuthor.Text,
        PackName = string.IsNullOrEmpty(ThemePackName.Text) ? string.Empty : ThemePackName.Text,
        PackDescription = string.IsNullOrEmpty(ThemePackDescription.Text) ? string.Empty : ThemePackDescription.Text,
        PackIconFileName = string.IsNullOrEmpty(ThemePackIcon.Text) ? string.Empty : ThemePackIcon.Text
    };

    private async void ImportPackIconButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window == null) return;

        var fileTypes = new List<FilePickerFileType>
        {
            FilePickerFileTypes.ImageAll
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            FileTypeFilter = fileTypes,
            AllowMultiple = false
        });

        if (files != null && files.Count > 0)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;

            ThemePackIcon.Text = filePath;
        }
    }
}