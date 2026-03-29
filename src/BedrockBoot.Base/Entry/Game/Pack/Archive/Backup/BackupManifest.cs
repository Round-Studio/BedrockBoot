using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Backup;

public class BackupManifest
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }
    
    [JsonPropertyName("updataTime")]
    public long UpdateTime { get; set; }
    
    [JsonPropertyName("icon")]
    public string Icon { get; set; }
    
    [JsonPropertyName("archiveName")]
    public string ArchiveName { get; set; }

    [JsonPropertyName("gameFolder")] public string GameFolder { get; set; }

    [JsonPropertyName("backups")] public List<BackupInfo> Backups { get; set; } = new();
    [JsonIgnore] public string BackupFolder { get; set; }
    
    public class BackupInfo
    {
        [JsonPropertyName("folder")] public string FolderID { get; set; }
        [JsonPropertyName("backupName")] public string BackupName { get; set; } = "新建备份";
        [JsonPropertyName("backupTime")] public long BackupTime { get; set; }
    }
}