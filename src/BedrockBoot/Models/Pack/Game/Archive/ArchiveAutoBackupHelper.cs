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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Pack.Archive;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;

namespace BedrockBoot.Models.Pack.Game.Archive;

public enum AutoBackupTriggerTiming
{
    Launch,
    Exit
}

public static class ArchiveAutoBackupHelper
{
    private static readonly object LockObj = new();

    /// <summary>
    /// 检查指定实例中所有已修改的世界存档，并在后台执行自动备份
    /// </summary>
    /// <param name="versionConfig">当前实例配置</param>
    /// <param name="timing">触发时机（Launch 或 Exit）</param>
    public static async Task AutoBackupModifiedArchivesAsync(VersionConfig versionConfig, AutoBackupTriggerTiming timing = AutoBackupTriggerTiming.Launch)
    {
        if (versionConfig == null) return;

        var config = BedrockBoot.Core.Global.GlobalModel.Config?.Data;
        if (config == null || !config.IsAutoBackupArchive) return;

        if (timing == AutoBackupTriggerTiming.Launch && !config.IsAutoBackupOnLaunch) return;
        if (timing == AutoBackupTriggerTiming.Exit && !config.IsAutoBackupOnExit) return;

        var timingKey = timing == AutoBackupTriggerTiming.Launch
            ? "Task.Archive.AutoBackup.Timing.Launch"
            : "Task.Archive.AutoBackup.Timing.Exit";
        var timingDefault = timing == AutoBackupTriggerTiming.Launch ? "启动" : "退出";
        var triggerTiming = I18nManager.Instance[timingKey] ?? timingDefault;

        try
        {
            var archiveCheck = new ArchiveCheck(versionConfig);
            var manifest = archiveCheck.Check();
            if (manifest?.Manifest == null) return;

            var allArchives = manifest.Manifest.Values.SelectMany(x => x).ToList();
            if (allArchives.Count == 0) return;

            var maxCount = config.AutoBackupMaxCount > 0 ? config.AutoBackupMaxCount : 5;
            var backedUpCount = 0;

            foreach (var info in allArchives)
            {
                if (string.IsNullOrEmpty(info.Path) || !Directory.Exists(info.Path))
                    continue;

                if (!IsArchiveModified(info))
                    continue;

                var prefix = I18nManager.Instance["Task.Archive.AutoBackup.Prefix"] ?? "自动备份";
                var backupName = $"{prefix} ({triggerTiming}) {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                Console.WriteLine($@"[自动备份] 检测到存档已修改，正在自动备份: {info.Name} (UUID: {info.Uuid})");

                try
                {
                    await GlobalModel.ArchiveBackup.BackupAsync(info, backupName, new Progress<string>(_ => { }));
                    backedUpCount++;

                    CleanOldAutoBackups(info.Uuid, maxCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"[自动备份] 备份存档 {info.Name} 失败: {ex.Message}");
                }
            }

            if (backedUpCount > 0 && config.IsAutoBackupNotice)
            {
                var title = I18nManager.Instance["Task.Archive.AutoBackup.Notice.Title"] ?? "自动存档备份";
                var format = I18nManager.Instance["Task.Archive.AutoBackup.Notice.Message"] ?? "已自动备份 {0} 个已修改的世界存档。";
                var msg = string.Format(format, backedUpCount);

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
                        {
                            Title = title,
                            Message = msg,
                            NoticeType = NoticeType.Info
                        });
                    }
                    catch
                    {
                        // 忽略通知异常
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[自动备份] 自动备份流程执行异常: {ex}");
        }
    }

    /// <summary>
    /// 判断存档是否自上次备份以来发生过修改（若从未备份也视为需要备份）
    /// </summary>
    public static bool IsArchiveModified(ArchiveInfo info)
    {
        if (string.IsNullOrEmpty(info.Path) || !Directory.Exists(info.Path))
            return false;

        var lastWriteTimestamp = GetArchiveLatestModifiedTimestamp(info.Path);

        var backupManifest = GlobalModel.ArchiveBackup.GetArchiveBackupsWhitUuid(info.Uuid);
        if (backupManifest == null || backupManifest.Backups.Count == 0)
        {
            // 从未备份过，需要首次备份
            return true;
        }

        var latestBackupTime = backupManifest.Backups.Max(b => b.BackupTime);

        // 如果最后修改时间晚于最新备份时间（加2秒缓冲防止文件系统时间精度抖动），判定为修改过
        return lastWriteTimestamp > (latestBackupTime + 2);
    }

    /// <summary>
    /// 获取世界存档最新被写入的 Unix 时间戳（秒）
    /// </summary>
    public static long GetArchiveLatestModifiedTimestamp(string savePath)
    {
        try
        {
            var levelDat = Path.Combine(savePath, "level.dat");
            var latestTime = File.Exists(levelDat)
                ? File.GetLastWriteTimeUtc(levelDat)
                : Directory.GetLastWriteTimeUtc(savePath);

            var dbFolder = Path.Combine(savePath, "db");
            if (Directory.Exists(dbFolder))
            {
                var dbTime = Directory.GetLastWriteTimeUtc(dbFolder);
                if (dbTime > latestTime)
                    latestTime = dbTime;
            }

            return new DateTimeOffset(latestTime).ToUnixTimeSeconds();
        }
        catch
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// 清理超出上限的自动备份（仅清理以“自动备份”命名的条目，绝不删除用户手动创建的备份）
    /// </summary>
    public static void CleanOldAutoBackups(string archiveUuid, int maxCount)
    {
        if (maxCount <= 0) return;

        lock (LockObj)
        {
            try
            {
                var manifest = GlobalModel.ArchiveBackup.GetArchiveBackupsWhitUuid(archiveUuid);
                if (manifest == null || manifest.Backups.Count == 0) return;

                var currentPrefix = I18nManager.Instance["Task.Archive.AutoBackup.Prefix"] ?? "自动备份";
                var autoBackups = manifest.Backups
                    .Where(b => !string.IsNullOrEmpty(b.BackupName) && (
                        b.BackupName.StartsWith("自动备份") ||
                        b.BackupName.StartsWith("Auto-Backup") ||
                        b.BackupName.StartsWith("自動バックアップ") ||
                        b.BackupName.StartsWith(currentPrefix)))
                    .OrderBy(b => b.BackupTime)
                    .ToList();

                while (autoBackups.Count > maxCount)
                {
                    var oldest = autoBackups[0];
                    Console.WriteLine($@"[自动备份清理] 正在清理超出上限的早期自动备份: {oldest.BackupName} ({oldest.FolderID})");
                    GlobalModel.ArchiveBackup.DeleteArchiveBackup(archiveUuid, oldest.FolderID);
                    autoBackups.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"[自动备份清理] 清理早期备份发生异常: {ex.Message}");
            }
        }
    }
}
