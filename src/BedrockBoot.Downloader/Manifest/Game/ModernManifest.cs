using System.Text.Json.Serialization;

namespace BedrockBoot.Downloader.Manifest.Game;

public class ModernManifest
{
    [JsonPropertyName("versions")] public Versions[]? VersionsArray { get; set; }

    public class Versions
    {
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("versionHash")] public string VersionHash { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
        [JsonPropertyName("vid")] public string Vid { get; set; } = string.Empty;
        [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
        [JsonPropertyName("buildType")] public string BuildType { get; set; } = string.Empty;
    }
}