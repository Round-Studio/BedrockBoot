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
using BedrockBoot.Models.Helper;
using BedrockBoot.Proton.Entry.Info;

namespace BedrockBoot.Views.Control.Items.Proton;

public partial class InstalledProtonItem : UserControl
{
    private readonly ProtonInfo _info;
    private readonly Action _updateUi;

    public InstalledProtonItem()
    {
        InitializeComponent();
    }

    public InstalledProtonItem(ProtonInfo info, Action updateUi) : this()
    {
        _info = info;
        _updateUi = updateUi;
        UpdateUI();
    }

    public void UpdateUI()
    {
        Card.Description = $"{_info.Version}, 来自 {_info.Branch}";
        ProtonName.Text = _info.Name;
        IsDefault.IsVisible = _info.IsDefault;
        // DeleteProtonBtn.IsVisible = !_info.IsDefault;
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenFolderHelper.Open(_info.InstallPath);
    }
}