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

namespace BedrockBoot.Views.DialogContent.Plugin.LeviLamina;

public partial class DialogLeviLaminaChooseVersionContent : UserControl
{
    private readonly List<string> _versions;

    public DialogLeviLaminaChooseVersionContent()
    {
        InitializeComponent();
    }

    public DialogLeviLaminaChooseVersionContent(List<string> versions) : this()
    {
        _versions = versions;
        versions.ForEach(v => VersionList.Items.Add(new ListBoxItem
        {
            Content = v
        }));
        VersionList.SelectedIndex = 0;
    }

    public string Version => _versions[VersionList.SelectedIndex];
}