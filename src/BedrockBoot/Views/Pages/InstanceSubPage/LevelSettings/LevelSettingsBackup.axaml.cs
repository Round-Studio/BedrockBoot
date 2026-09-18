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

using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsBackup : UserControl
{
    private readonly ArchiveInfo _info;

    public LevelSettingsBackup()
    {
        InitializeComponent();
    }

    public LevelSettingsBackup(ArchiveInfo info) : this()
    {
        _info = info;
        UpdateBackup();
    }

    private void CreateBackup_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogCreateBackupContent();
        DialogHost.Show(new DialogInfo
        {
            Content = dialog,
            Title = "新建备份",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var name = dialog.BackupNameInfo;
                DialogHost.Show(new DialogInfo
                {
                    Title = "备份存档",
                    Content = new DialogBackupProgressContent(_info, name, () =>
                    {
                        UpdateBackup();
                        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                        {
                            Title = "存档备份",
                            Message = $"已成功备份 {name}",
                            NoticeType = NoticeType.Info
                        });
                    })
                });
            }
        });
    }

    private void UpdateBackup()
    {
        var uuid = _info.Uuid;
        var backups = GlobalModel.ArchiveBackup.GetArchiveBackupsWhitUuid(uuid);

        NullBox.IsVisible = false;
        LoadingCard.IsVisible = true;
        ArchiveScrollViewer.IsVisible = false;
        ArchivesBox.Children.Clear();

        if (backups == null)
        {
            NullBox.IsVisible = true;
            LoadingCard.IsVisible = false;
            return;
        }

        if (backups.Backups.Count == 0)
        {
            NullBox.IsVisible = true;
            LoadingCard.IsVisible = false;
            return;
        }

        backups.Backups.Reverse();

        backups.Backups.ForEach(x =>
        {
            ArchivesBox.Children.Add(new ArchiveBackupItem(_info, x, backups)
            {
                RefreshCallBack = UpdateBackup
            });
        });

        NullBox.IsVisible = false;
        LoadingCard.IsVisible = false;
        ArchiveScrollViewer.IsVisible = true;
    }
}