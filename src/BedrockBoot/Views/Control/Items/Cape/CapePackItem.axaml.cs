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
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BedrockBoot.Models.Pack.Game.Capes;

namespace BedrockBoot.Views.Control.Items.Cape;

public partial class CapePackItem : UserControl
{
    private readonly CapesPackUnit _unit;
    public Action? EditAction { get; set; }

    public CapePackItem()
    {
        InitializeComponent();
    }
    public CapePackItem(CapesPackUnit unit):this()
    {
        _unit = unit;
        
        SettingExpander.Header = _unit.SourceManifest?.Data.Header.Name ?? "Local Cape Pack";
        SettingExpander.Description = $"共 {_unit.LoadCapeFile().Count} 个披风文件";
        if (!string.IsNullOrEmpty(_unit.PackIcon))
        {
            if (File.Exists(_unit.PackIcon))
            {
                var bitmapIcon = new Bitmap(_unit.PackIcon);
                SettingExpander.IsFontIcon = false;
                SettingExpander.ImageIcon = bitmapIcon;
            }
        }
    }

    private void EditBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        EditAction?.Invoke();
    }
}