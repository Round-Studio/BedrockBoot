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
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Pack.Game.Archive;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogChooseGameWorldsContent : UserControl
{
    public DialogChooseGameWorldsContent()
    {
        InitializeComponent();
    }

    public DialogChooseGameWorldsContent(VersionConfig versionConfig) : this()
    {
        var worlds = new ArchiveCheck(versionConfig).Check().Manifest.Values.ToList();
        ArchiveInfos = worlds.SelectMany(w => w).ToList();

        ArchiveInfos.ForEach(w =>
        {
            WorldsList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = w.Name,
                    Margin = new Thickness(0, 0)
                }
            });
        });
    }

    public List<ArchiveInfo> ArchiveInfos { get; }

    public ArchiveInfo? SelectedArchiveInfo =>
        ArchiveInfos.Count == 0 ? null : ArchiveInfos[WorldsList.SelectedIndex];
}