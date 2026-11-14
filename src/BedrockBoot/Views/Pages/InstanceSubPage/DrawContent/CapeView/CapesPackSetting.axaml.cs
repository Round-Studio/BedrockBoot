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
    private readonly CapesPackUnit _unit;

    public CapesPackSetting()
    {
        InitializeComponent();
    }

    public CapesPackSetting(CapesPackUnit unit) : this()
    {
        _unit = unit;
        UpdateUI();
    }

    public Action? BackAction { get; set; }

    public void UpdateUI()
    {
        PackNameLabel.Text = _unit.SourceManifest.Data.Header.Name;
        NavigationFrame.NavigateTo(new CapesFileView(_unit));
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        BackAction?.Invoke();
    }

    private void NavBar_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavBar?.SelectedItem is not ListBoxItem item) return;
        var tag = item.Tag?.ToString();
        object? page = tag switch
        {
            "Capes" => new CapesFileView(_unit),
            "Setting" => new CapesPackInfoPage(_unit),
            _ => null
        };

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}