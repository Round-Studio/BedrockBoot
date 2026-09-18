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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogBackupProgressContent : UserControl
{
    private readonly string _backupName;
    private readonly ArchiveInfo _info;
    private readonly Action _success;

    public DialogBackupProgressContent()
    {
        InitializeComponent();
    }

    public DialogBackupProgressContent(ArchiveInfo info, string backupName, Action success) : this()
    {
        _info = info;
        _backupName = backupName;
        _success = success;
        Backup();
    }

    public async Task Backup()
    {
        await GlobalModel.ArchiveBackup.BackupAsync(_info, _backupName, new Progress<string>(s =>
        {
            Console.WriteLine($@"备份进度：{s}");
            Dispatcher.UIThread.Invoke(() => { ProgressText.Text = $"备份进度：{s} %"; });
        }));

        DialogHost.Close();
        _success.Invoke();
    }
}