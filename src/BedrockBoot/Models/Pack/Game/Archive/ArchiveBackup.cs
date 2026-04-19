using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Entry.Game.Pack.Archive.Backup;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper.IO;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveBackup
{
    public ArchiveBackup()
    {
        IndexConfig = new ConfigEntity<BackupIndex>(Path.Combine(PathsList.ArchiveBackup, "index.json"));
    }

    public ConfigEntity<BackupIndex> IndexConfig { get; set; }

    public async Task BackupAsync(ArchiveInfo info, string backupName, IProgress<string> progress,
        CancellationToken cancellationToken = default)
    {
        if (!IndexConfig.Data.Index.Contains(info.Uuid)) IndexConfig.Data.Index.Add(info.Uuid);

        IndexConfig.Data.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        IndexConfig.Save();

        var indexFolder = Path.Combine(PathsList.ArchiveBackup, "backups", info.Uuid);
        Directory.CreateDirectory(indexFolder);

        if (File.Exists(info.IconPath)) File.Copy(info.IconPath, Path.Combine(indexFolder, "icon.jpeg"), true);

        var newBackupUuid = Guid.NewGuid().ToString("N");
        var backupFolder = Path.Combine(indexFolder, newBackupUuid);
        Directory.CreateDirectory(backupFolder);

        var conf = new ConfigEntity<BackupManifest>(Path.Combine(indexFolder, "manifest.json"));
        conf.Data.Uuid = info.Uuid;
        conf.Data.ArchiveName = info.LevelWorldData.LevelName;
        conf.Data.GameFolder = info.VersionInfo.VersionPath;
        conf.Data.UpdateTime = IndexConfig.Data.UpdateTime;
        conf.Data.Icon = "icon.jpeg";
        conf.Data.Backups.Add(new BackupManifest.BackupInfo
        {
            BackupTime = conf.Data.UpdateTime,
            FolderID = newBackupUuid,
            BackupName = backupName
        });
        conf.Save();

        // 使用异步复制方法
        await FolderCopier.CopyAsync(
            info.Path,
            backupFolder,
            new Progress<(int current, int total, string file, long copied, long totalBytes)>(p =>
            {
                var percentage = p.totalBytes > 0
                    ? (double)p.copied / p.totalBytes * 100
                    : 0;

                progress.Report($"{percentage:F2}%");
            }),
            true,
            true,
            cancellationToken
        );
    }

    public BackupManifest? GetArchiveBackupsWhitUuid(string archiveUuid)
    {
        IndexConfig.Load();
        if (!IndexConfig.Data.Index.Contains(archiveUuid))
            return null;

        var folder = Path.Combine(PathsList.ArchiveBackup, "backups", archiveUuid);
        var manifestFile = Path.Combine(folder, "manifest.json");

        var manifest = new ConfigEntity<BackupManifest>(manifestFile, false);
        manifest.Data.Icon = Path.Combine(folder, manifest.Data.Icon);
        manifest.Data.BackupFolder = folder;
        return manifest.Data;
    }

    public void RollbackArchiveBackup(ArchiveInfo info, string backupId)
    {
        var folder = info.Path;
        try
        {
            Directory.Delete(folder, true);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        FolderCopier.Copy(
            Path.Combine(GetArchiveBackupsWhitUuid(info.Uuid)!.BackupFolder, backupId),
            folder);
    }

    public void DeleteArchiveBackup(string archiveUuid, string backupId)
    {
        if (!IndexConfig.Data.Index.Contains(archiveUuid))
            throw new NullReferenceException();

        var indexFolder = Path.Combine(PathsList.ArchiveBackup, "backups", archiveUuid);
        var conf = new ConfigEntity<BackupManifest>(Path.Combine(indexFolder, "manifest.json"));

        conf.Data.Backups.RemoveAt(conf.Data.Backups.FindIndex(x => x.FolderID == backupId));
        conf.Save();

        try
        {
            Directory.Delete(Path.Combine(indexFolder, backupId), true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}