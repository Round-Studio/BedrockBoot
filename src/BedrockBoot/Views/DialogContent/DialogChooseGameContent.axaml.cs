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
using System.Collections.Generic;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Isolation;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogChooseGameContent : ISetting
{
    private List<VersionConfig>? Versions;
    public string SelectUser => Users[InstanceUsers.SelectedIndex];
    public List<string> Users { get; set; }

    public DialogChooseGameContent()
    {
        InitializeComponent();
        Update();
    }

    public DialogChooseGameContent(bool isEnableChooseUser = false) : this() => UserBox.IsVisible = isEnableChooseUser;

    public VersionConfig? VersionConfig
    {
        get
        {
            var index = GameInstance.SelectedIndex;
            if (Versions is null || index < 0 || index >= Versions.Count)
                return null;
            return Versions[index];
        }
    }

    public void Update()
    {
        IsEnabled = false;
        try
        {
            GameFolder.Items.Clear();
            GlobalModel.Config.Data.GameFolders.ForEach(f =>
            {
                GameFolder.Items.Add(new ComboBoxItem
                {
                    Content = $"{f.GameFolderName} - {f.GameFolderPath}"
                });
            });

            var folderIndex = GlobalModel.Config.Data.GameFolderSelIndex;
            GameFolder.SelectedIndex = (folderIndex >= 0 && folderIndex < GameFolder.Items.Count)
                ? folderIndex
                : (GameFolder.Items.Count > 0 ? 0 : -1);

            UpdateList();
            UpdateUsers();
        }
        finally
        {
            IsEnabled = true;
        }
    }

    public void UpdateList()
    {
        GameInstance.Items.Clear();

        var folderIndex = GameFolder.SelectedIndex;
        if (folderIndex < 0 || folderIndex >= GlobalModel.Config.Data.GameFolders.Count)
        {
            Versions = new List<VersionConfig>();
            GameInstance.SelectedIndex = -1;
            return;
        }

        var path = GlobalModel.Config.Data.GameFolders[folderIndex].GameFolderPath;
        Versions = GameInfoHelper.GetVersionConfigs(path) ?? new List<VersionConfig>();

        foreach (var v in Versions)
        {
            GameInstance.Items.Add(new ComboBoxItem
            {
                Content = $"{v.Info.VersionName} - {v.Info.Version}",
                Tag = v
            });
        }

        var savedIndex = GlobalModel.Config.Data.GameFolders[folderIndex].GameSelIndex;
        GameInstance.SelectedIndex = (savedIndex >= 0 && savedIndex < Versions.Count)
            ? savedIndex
            : (Versions.Count > 0 ? 0 : -1);
    }

    private void GameFolder_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateList();

    private void GameInstance_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GameInstance == null || GameInstance.SelectedIndex < 0) return;
        if (Versions == null || Versions.Count == 0) return;
        if (GameInstance.SelectedIndex >= Versions.Count) return;
        UpdateUsers();
    }

    private void UpdateUsers()
    {
        if (Versions == null || Versions.Count == 0)
        {
            InstanceUsers.ItemsSource = new List<string>();
            return;
        }

        var selectedIndex = GameInstance?.SelectedIndex ?? -1;
        if (selectedIndex < 0 || selectedIndex >= Versions.Count)
        {
            InstanceUsers.ItemsSource = new List<string>();
            return;
        }

        var instance = Versions[selectedIndex];
        Users = IsolationCore.GetInstanceUsers(instance) ?? new List<string>();
        InstanceUsers.ItemsSource = Users;
        InstanceUsers.SelectedIndex = Users.Count > 0 ? 0 : -1;
    }
}