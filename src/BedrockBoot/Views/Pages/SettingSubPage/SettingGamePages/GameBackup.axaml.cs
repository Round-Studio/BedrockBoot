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
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using CoreGlobal = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameBackup : ISettingPage
{
    public GameBackup()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "存档备份"
            }
        };

        UpdateUI();
    }

    public void UpdateUI()
    {
        IsEdit = false;

        var config = CoreGlobal.Config.Data;
        AutoBackupSwitch.IsChecked = config.IsAutoBackupArchive;
        OnLaunchSwitch.IsChecked = config.IsAutoBackupOnLaunch;
        OnExitSwitch.IsChecked = config.IsAutoBackupOnExit;
        NoticeSwitch.IsChecked = config.IsAutoBackupNotice;
        BackupOptionsPanel.IsEnabled = config.IsAutoBackupArchive;

        MaxCountBox.SelectedIndex = config.AutoBackupMaxCount switch
        {
            3 => 0,
            5 => 1,
            10 => 2,
            20 => 3,
            <= 0 => 4,
            _ => 1
        };

        IsEdit = true;
    }

    private void AutoBackupSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;
        var isChecked = AutoBackupSwitch.IsChecked ?? false;
        CoreGlobal.Config.Data.IsAutoBackupArchive = isChecked;
        BackupOptionsPanel.IsEnabled = isChecked;
        CoreGlobal.Config.Save();
    }

    private void OnLaunchSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;
        CoreGlobal.Config.Data.IsAutoBackupOnLaunch = OnLaunchSwitch.IsChecked ?? false;
        CoreGlobal.Config.Save();
    }

    private void OnExitSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;
        CoreGlobal.Config.Data.IsAutoBackupOnExit = OnExitSwitch.IsChecked ?? false;
        CoreGlobal.Config.Save();
    }

    private void MaxCountBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;
        CoreGlobal.Config.Data.AutoBackupMaxCount = MaxCountBox.SelectedIndex switch
        {
            0 => 3,
            1 => 5,
            2 => 10,
            3 => 20,
            4 => 0, // 0 表示不限制
            _ => 5
        };
        CoreGlobal.Config.Save();
    }

    private void NoticeSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!IsEdit) return;
        CoreGlobal.Config.Data.IsAutoBackupNotice = NoticeSwitch.IsChecked ?? false;
        CoreGlobal.Config.Save();
    }

    private void OpenBackupFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(PathsList.ArchiveBackup))
            Directory.CreateDirectory(PathsList.ArchiveBackup);

        OpenFolderHelper.Open(PathsList.ArchiveBackup);
    }
}