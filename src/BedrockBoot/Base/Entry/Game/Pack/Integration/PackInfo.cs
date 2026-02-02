using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Integration;

public class PackInfo
{
    [JsonPropertyName("packVersion")] public int PackVersion { get; set; } = 1;
    [JsonPropertyName("name")] public string Name { get; set; } = "pack.name";
    [JsonPropertyName("description")] public string Description { get; set; } = "pack.description";
    [JsonPropertyName("version")] public string Version { get; set; } = "0.0.0.1";
    [JsonPropertyName("author")] public List<PackAuthor> Authors { get; set; }
    [JsonPropertyName("versionInfo")] public GameVersionInfo VersionInfo { get; set; }
    [JsonIgnore] public PackEnableConfig EnableConfig { get; set; } = new();
    [JsonIgnore] public string PackIconFile { get; set; } = string.Empty;
    [JsonIgnore] public string PackSavePath { get; set; } = string.Empty;
    
    public class GameVersionInfo
    {
        [JsonPropertyName("buildType")] public string BuildType { get; set; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    }
}