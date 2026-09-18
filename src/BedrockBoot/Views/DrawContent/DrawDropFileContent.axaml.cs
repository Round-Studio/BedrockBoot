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

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Views.Control.Items.System;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDropFileContent : UserControl
{
    private readonly IStorageItem[] _storageItems;

    public DrawDropFileContent()
    {
        InitializeComponent();
    }

    public DrawDropFileContent(IStorageItem[] storageItems) : this()
    {
        _storageItems = storageItems;
        UpdateUi();
    }

    public void UpdateUi()
    {
        FileCount.Text = $"已接收 {_storageItems.Length} 个文件";
        FilesPanel.Children.Clear();
        FilesPanel.Children.AddRange(_storageItems.Select(file => new DropFileItem(file)));
    }
}