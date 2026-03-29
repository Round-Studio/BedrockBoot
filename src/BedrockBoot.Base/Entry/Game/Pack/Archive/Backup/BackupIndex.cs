using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Backup;

public class BackupIndex
{
    [JsonPropertyName("updateTime")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("index")] public List<string> Index { get; set; } = new();
}