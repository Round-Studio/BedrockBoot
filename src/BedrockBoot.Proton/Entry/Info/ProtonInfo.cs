using System.Text.Json.Serialization;

namespace BedrockBoot.Proton.Entry.Info;

public class ProtonInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonPropertyName("isDefault")] public bool IsDefault { get; set; } = false;
    [JsonPropertyName("installDate")] public DateTime InstallDate { get; set; } = DateTime.Now;
    [JsonPropertyName("releaseUrl")] public string ReleaseUrl { get; set; } = string.Empty;
    [JsonPropertyName("releaseSize")] public long ReleaseSize { get; set; } = 0;
    [JsonIgnore] public string InstallPath { get; set; } = string.Empty;
}