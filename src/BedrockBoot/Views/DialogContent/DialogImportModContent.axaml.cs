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

using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportModContent : UserControl
{
    public DialogImportModContent()
    {
        InitializeComponent();
    }

    public string ModFile
    {
        get => PathInputBox.Text;
        set => PathInputBox.Text = value;
    }

    public int ModDelay
    {
        get => (int)InjectionDelay.Value;
        set => InjectionDelay.Value = value;
    }

    public bool IsPreLoad
    {
        get => EnablePreLoad.IsChecked ?? false;
        set => EnablePreLoad.IsChecked = value;
    }

    private async void OpenChooseFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "请选择 DLL 文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("DLL 文件")
                {
                    Patterns = new[] { "*.dll" }
                }
            }
        });

        if (files != null && files.Count >= 1)
        {
            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath)) PathInputBox.Text = filePath;
        }
    }

    private void EnablePreLoad_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        DelayPanel.IsVisible = !IsPreLoad;
    }
}