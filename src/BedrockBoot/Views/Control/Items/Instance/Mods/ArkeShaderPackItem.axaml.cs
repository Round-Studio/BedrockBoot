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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Game.Mods.Arke;

namespace BedrockBoot.Views.Control.Items.Instance.Mods;

public partial class ArkeShaderPackItem : UserControl
{
    private readonly ArkeShaderPackInfo _info;
    private readonly Action<string> _deleteAction;

    public ArkeShaderPackItem()
    {
        InitializeComponent();
    }

    public ArkeShaderPackItem(ArkeShaderPackInfo info, Action<string> deleteAction) : this()
    {
        _info = info;
        _deleteAction = deleteAction;

        Header.MinecraftText = info.PackName;
        Card.Description = FormatSize(info.PackSize);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";

        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;
        
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        string format = number < 10 && counter > 0 ? "0.##" : "0.#";
        return $"{number.ToString(format)} {suffixes[counter]}";
    }

    private void SettingCard_OnClick(object? sender, RoutedEventArgs e)
    {
        _deleteAction.Invoke(_info.PackPath);
    }
}