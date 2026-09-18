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
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game.Pack.Integration;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeIntegrationPackConfigContent : UserControl
{
    public DialogMakeIntegrationPackConfigContent()
    {
        InitializeComponent();
    }

    public PackInfo PackConfig => new()
    {
        Authors = new List<PackAuthor>
        {
            new()
            {
                Name = string.IsNullOrEmpty(PackAuthorName.Text) ? "Creator" : PackAuthorName.Text,
                Links = new List<string> { string.IsNullOrEmpty(PackAuthorLink.Text) ? "" : PackAuthorLink.Text }
            }
        },
        Name = string.IsNullOrEmpty(PackName.Text) ? "My Pack" : PackName.Text,
        Version = string.IsNullOrEmpty(PackVersion.Text) ? "0.0.0.1" : PackVersion.Text,
        Description = string.IsNullOrEmpty(PackDescription.Text) ? "My Pack Description " : PackDescription.Text,
        EnableConfig = new PackEnableConfig
        {
            IsEnableArchive = (bool)EnableArchive.IsChecked!,
            IsEnableBehaviorPack = (bool)EnableBehaviorPack.IsChecked!,
            IsEnableDllFile = (bool)EnableDllMods.IsChecked!,
            IsEnableResourcePack = (bool)EnableResourcePack.IsChecked!
        }
    };
}