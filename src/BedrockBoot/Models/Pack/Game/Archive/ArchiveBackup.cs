using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Entry.Game.Pack.Archive.Backup;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveBackup
{
    public ConfigEntity<BackupIndex> IndexConfig { get; set; }
    public ArchiveBackup()
    {
        IndexConfig = new ConfigEntity<BackupIndex>(Path.Combine(PathsList.ArchiveBackup, "index.json"));
    }

    public void Backup(ArchiveInfo info, Action backupComplete, IProgress<string> progress)
    {
        if (!IndexConfig.Data.Index.Contains(info.Uuid))
        {
            IndexConfig.Data.Index.Add(info.Uuid);
        }

        IndexConfig.Data.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        IndexConfig.Save();

        var indexFolder = Path.Combine(PathsList.ArchiveBackup, "backups", info.Uuid);
        Directory.CreateDirectory(indexFolder);

        if (File.Exists(Path.Combine(info.IconPath)))
        {
            File.Copy(info.IconPath, Path.Combine(indexFolder, "icon.jpeg"), true);
        }

        var newBackupUuid = Guid.NewGuid().ToString("N");

        Directory.CreateDirectory(Path.Combine(indexFolder, newBackupUuid));

        var conf = new ConfigEntity<BackupManifest>(Path.Combine(indexFolder, "manifest.json"));
        conf.Data.Uuid = info.Uuid;
        conf.Data.ArchiveName = info.LevelWorldData.LevelName;
        conf.Data.GameFolder = info.VersionInfo.VersionPath;
        conf.Data.UpdateTime = IndexConfig.Data.UpdateTime;
        conf.Data.Icon = "icon.jpeg";
        conf.Data.Backups.Add(new BackupManifest.BackupInfo()
        {
            BackupTime = conf.Data.UpdateTime,
            FolderID = newBackupUuid
        });
        conf.Save();

        new Thread(() =>
        {
            
        }).Start();
    }

    public BackupManifest? GetArchiveBackupsWhitUuid(string archiveUuid)
    {
        if (!IndexConfig.Data.Index.Contains(archiveUuid))
            return null;

        var folder = Path.Combine(PathsList.ArchiveBackup, "backups", archiveUuid);
        var manifestFile = Path.Combine(folder, "manifest.json");

        var manifest = new ConfigEntity<BackupManifest>(manifestFile, false);
        manifest.Data.Icon = Path.Combine(folder, manifest.Data.Icon);
        return manifest.Data;
    }
}