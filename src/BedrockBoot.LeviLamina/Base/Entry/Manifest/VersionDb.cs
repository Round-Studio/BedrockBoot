using System.Text.Json.Serialization;

namespace BedrockBoot.LeviLamina.Base.Entry.Manifest;

public class VersionDb
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }
    [JsonPropertyName("versions")] public Dictionary<string,List<string>> Versions { get; set; }
}