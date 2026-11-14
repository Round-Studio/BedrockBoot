using System.Text.Json.Serialization;

namespace BedrockBoot.Standard.Entity.Game.Pack.Capes.Manifest;

public class CapePackManifest
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; } = 1;
    [JsonPropertyName("header")] public CapePackHeader Header { get; set; } = new();
    [JsonPropertyName("modules")] public List<CapePackModule> Modules { get; set; } = new();
    [JsonPropertyName("metadata")] public CapePackMetadata? Metadata { get; set; }
}

public class CapePackHeader
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("version")] public List<int> Version { get; set; } = new();
}

public class CapePackModule
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("version")] public List<int> Version { get; set; } = new();
}

public class CapePackMetadata
{
    [JsonPropertyName("authors")] public List<string> Authors { get; set; } = new();
}