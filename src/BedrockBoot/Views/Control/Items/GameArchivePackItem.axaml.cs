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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class GameArchivePackItem : UserControl
{
    private readonly ResourcePackManifest _info;
    private readonly bool _isAct;
    private readonly ImageLoader _imageLoader = ImageLoader.Shared;
    public Action<ResourcePackManifest>? ActiveAction { get; set; }

    public GameArchivePackItem()
    {
        InitializeComponent();
    }
    public GameArchivePackItem(ResourcePackManifest info,bool isAct):this()
    {
        _info = info;
        _isAct = isAct;
        _ = UpdateUi();
    }

    public async Task UpdateUi()
    {
        CheckBtn.IsVisible = !_isAct;
        CancelBtn.IsVisible = _isAct;

        var info = _info;
        
        if (info == null) return;

        try
        {
            if (info.PackIconBytes != null)
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                using var ms = new MemoryStream(info.PackIconBytes);
                // 图标只显示 32x32，按 64 宽解码，避免原图整张解码占内存
                Card.ImageIcon = Bitmap.DecodeToWidth(ms, 64);
            }
            else if (!string.IsNullOrEmpty(info.PackIcon))
            {
                if (Card.ImageIcon is IDisposable disposable) disposable.Dispose();

                Card.ImageIcon = await _imageLoader.LoadIconAsync(info.PackIcon, 64);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load pack icon: {ex.Message}");
        }

        PackName.MinecraftText = info.Header.Name;
        PackDescription.MinecraftText = string.IsNullOrEmpty(info.Header.Description)
            ? "该包还没有介绍..."
            : info.Header.Description;
        if (!string.IsNullOrEmpty(info.Header.Version))
            PackVersion.Text = info.Header.Version;
        GameVersion.IsVisible = !string.IsNullOrEmpty(info.Header.Version);
        if (!string.IsNullOrEmpty(info.Header.MinEngineVersion))
            GameVersion.Text = $"Minecraft {info.Header.MinEngineVersion}";
        GameVersion.IsVisible = !string.IsNullOrEmpty(info.Header.MinEngineVersion);
    }

    private void ActBtn_Click(object? sender, RoutedEventArgs e)
    {
        ActiveAction?.Invoke(_info);
    }
}