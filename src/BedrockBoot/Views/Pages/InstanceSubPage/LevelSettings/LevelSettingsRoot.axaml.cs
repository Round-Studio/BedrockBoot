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
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Interface;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsRoot : ISetting
{
    private readonly ArchiveInfo _info;
    private bool _isInternalUpdating = false;

    public LevelSettingsRoot()
    {
        InitializeComponent();
    }

    public LevelSettingsRoot(ArchiveInfo info) : this()
    {
        _info = info;
        UpdateUI();
    }

    public Action? BackAction { get; set; }

    private void UpdateUI()
    {
        NavigationFrame.NavigateTo(new LevelSettingsEditor(_info));
        LevelNameLabel.Text = _info.LevelWorldData.LevelName;
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        BackAction?.Invoke();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var tag = ((ListBoxItem)NavBar?.SelectedItem!).Tag?.ToString();
        object? page = null;
        if (tag != null)
            switch (tag)
            {
                case "Backup":
                    page = new LevelSettingsBackup(_info);
                    break;
                case "Setting":
                    page = new LevelSettingsEditor(_info);
                    break;
                case "Controls":
                    page = new LevelSettingsControls(_info);
                    break;
                case "Addon":
                    page = new LevelSettingsPack(_info);
                    break;
            }

        if (page != null) NavigationFrame.NavigateTo(page);
    }
}