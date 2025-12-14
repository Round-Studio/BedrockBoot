using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive;

public class ArchiveInfo
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("path")] public string Path { get; set; }
    [JsonPropertyName("iconPath")] public string IconPath { get; set; }
    [JsonPropertyName("isProject")] public bool IsProject { get; set; } = false;
}