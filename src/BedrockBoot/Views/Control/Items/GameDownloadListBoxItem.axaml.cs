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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Control.Items;

public partial class GameDownloadListBoxItem : UserControl
{
    private readonly BuildInfo _buildInfo;

    public GameDownloadListBoxItem()
    {
        InitializeComponent();
    }
    public GameDownloadListBoxItem(BuildInfo buildInfo):this()
    {
        _buildInfo = buildInfo;
        VersionName.Text = buildInfo.ID;
        VersionD.Text = $"{buildInfo.BuildType}, {buildInfo.Type}, {buildInfo.Date}";
        IconBox.ImageUrl = buildInfo.Type == MinecraftGameTypeVersion.Release
            ? "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png"
            : "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";
    }
}