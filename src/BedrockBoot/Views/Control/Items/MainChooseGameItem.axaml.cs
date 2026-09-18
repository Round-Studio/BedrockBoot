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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class MainChooseGameItem : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    private readonly VersionConfig _versionConfig;

    public MainChooseGameItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public MainChooseGameItem(VersionConfig versionConfig) : this()
    {
        _versionConfig = versionConfig;
        Update(_versionConfig);
    }

    public async Task Update(VersionConfig versionConfig)
    {
        GameInfo.Text = $"{versionConfig.Info.VersionType} {versionConfig.Info.Version}";
        GameName.Text = versionConfig.Info.VersionName;
        GameBuildType.Text = versionConfig.Info.BuildType.ToString();
        GameIcon.Source = await _imageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(versionConfig));
    }
}