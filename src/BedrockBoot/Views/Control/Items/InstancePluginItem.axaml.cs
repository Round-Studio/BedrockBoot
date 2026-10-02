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
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using BedrockBoot.Core.Interface.Instance;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class InstancePluginItem : UserControl
{
	private readonly ImageLoader _imageLoader = ImageLoader.Shared;
    public InstancePluginItem()
    {
        InitializeComponent();
    }

    public InstancePluginItem(IInstancePlugin plugin) : this()
    {
        InstancePlugin = plugin;
        _ = UpdateUIAsync();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public IInstancePlugin? InstancePlugin { get; set; }

    public async Task UpdateUIAsync()
    {
        if (InstancePlugin == null) return;

        // 状态显示
        if (InstancePlugin.IsInstalled())
        {
            InstalledBox.Background = Brushes.Orange;
            InstalledBox.Text = i18n["Instance.Plugin.Status.Installed"];
        }
        else
        {
            InstalledBox.IsVisible = false;
        }

        CardHeader.Text = InstancePlugin.Name;
        CardDescription.Text = InstancePlugin.Description;

        // 图标异步加载
        if (!string.IsNullOrEmpty(InstancePlugin.Icon))
            try
            {
                // 图标只显示 32x32，按 64 宽解码
                var icon = await _imageLoader.LoadIconAsync(InstancePlugin.Icon, 64);
                if (icon != null)
                {
                    Card.IsFontIcon = false;
                    Card.ImageIcon = icon;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Failed to load plugin icon: {ex.Message}");
            }
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        InstancePlugin?.Install();
    }
}