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
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Standard.Entity.Game.Pack.Archive.Backup;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Standard.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class ArchiveBackupItem : UserControl
{
    private readonly ArchiveInfo _archiveInfo;
    private readonly BackupManifest _manifest;

    public ArchiveBackupItem()
    {
        InitializeComponent();
    }

    public ArchiveBackupItem(ArchiveInfo archiveInfo, BackupManifest.BackupInfo info, BackupManifest manifest) : this()
    {
        _archiveInfo = archiveInfo;
        _manifest = manifest;
        BackupInfo = info;
        UpdateUI();
    }

    private static I18nManager i18n => I18nManager.Instance;
    public BackupManifest.BackupInfo? BackupInfo { get; set; }
    public Action? RefreshCallBack { get; set; }

    /// <summary>
    ///     更新存档卡片 UI
    /// </summary>
    public void UpdateUI()
    {
        if (BackupInfo == null) return;

        BackupName.Text = BackupInfo.BackupName;

        // 时间转换与格式化
        var lastPlayedTime = UnixTimeConverter.UnixTimeStampToDateTime(BackupInfo.BackupTime);
        BackupLastPlayed.Text = string.Format(I18nManager.Instance["UI.Item.BackupLastPlayed"], lastPlayedTime.ToString("yyyy/MM/dd HH:mm"));

        // 异步或流式加载图标，防止文件占用
        LoadBackupIcon(ArchiveCheck.GetInfo(Path.Combine(_manifest.BackupFolder, BackupInfo.FolderID),
            _manifest.GameFolder)!.IconPath);
    }

    private void LoadBackupIcon(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            using var stream = File.OpenRead(path);
            var bitmap = new Bitmap(stream);
            ImageBox.Background = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                Source = bitmap
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to load Backup icon: {ex.Message}");
        }
    }

    /// <summary>
    ///     在资源管理器中打开存档目录
    /// </summary>
    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        PlatformCore.OpenFolder!.Open(Path.Combine(_manifest.BackupFolder, BackupInfo.FolderID));
    }

    /// <summary>
    ///     导出存档为 .mcworld 包
    /// </summary>
    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = i18n["Archive.Export.Title"],
            DefaultExtension = "mcworld",
            SuggestedFileName = $"{BackupInfo.BackupName}.mcworld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(i18n["Archive.Export.FileType"])
                {
                    Patterns = new[] { "*.mcworld" }
                }
            }
        });

        var localPath = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(localPath)) return;

        try
        {
            ArchiveCheck.GetInfo(Path.Combine(_manifest.BackupFolder, BackupInfo.FolderID),
                _manifest.GameFolder)!.Save(localPath);
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["Common.Success"],
                Message = i18n["Archive.Export.Success"],
                NoticeType = NoticeType.Info
            });
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Message = ex.Message,
                NoticeType = NoticeType.Error
            });
        }
    }

    private void BackupBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Archive.Restore.Warn.Title"],
            Content = I18nManager.Instance["Archive.Restore.Warn.Content"],
            CloseButtonText = I18nManager.Instance["Archive.Restore.Warn.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            CloseAction = () => { GlobalModel.ArchiveBackup.RollbackArchiveBackup(_archiveInfo, BackupInfo.FolderID); }
        });
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Archive.Delete.Warn.Title"],
            Content = I18nManager.Instance["Archive.Delete.Warn.Content.Backup"],
            CloseButtonText = I18nManager.Instance["Archive.Delete.Warn.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            CloseAction = () =>
            {
                if (BackupInfo != null)
                    GlobalModel.ArchiveBackup.DeleteArchiveBackup(_manifest.Uuid, BackupInfo.FolderID);

                RefreshCallBack?.Invoke();
            }
        });
    }
}