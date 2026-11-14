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
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.CapeView;

public partial class CapesPackSetting : UserControl
{
    private readonly VersionConfig _versionInfo;
    private readonly string _folderName;

    public CapesPackSetting()
    {
        InitializeComponent();
    }

    public CapesPackSetting(VersionConfig versionInfo, string folderName) : this()
    {
        _versionInfo = versionInfo;
        _folderName = folderName;
        UpdateUI();
    }

    public Action? BackAction { get; set; }

    public void UpdateUI()
    {
        var pack = CapesPackManager.LoadPacks(_versionInfo.VersionPath)
            .Find(p => p.FolderName == _folderName);
        PackNameLabel.Text = pack?.PackName ?? _folderName;
        NavigationFrame.NavigateTo(new CapesFileView(_versionInfo, _folderName));
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        BackAction?.Invoke();
    }

    private void NavBar_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavBar?.SelectedItem is not ListBoxItem item) return;
        if (_versionInfo == null) return;
        var tag = item.Tag?.ToString();
        object? page = tag switch
        {
            "Capes" => new CapesFileView(_versionInfo, _folderName),
            "Setting" => new CapesPackInfoPage(_versionInfo, _folderName),
            _ => null
        };

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}