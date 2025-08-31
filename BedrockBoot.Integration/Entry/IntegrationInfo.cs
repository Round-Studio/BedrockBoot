using System.Text.Json.Serialization;

namespace BedrockBoot.Integration.Entry;

public class IntegrationInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("author")] public string Author { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonIgnore] public bool UseMods { get; set; } = true;
    [JsonIgnore] public bool UseDMods { get; set; } = true;
    [JsonIgnore] public bool UseWorlds { get; set; } = true;
    [JsonIgnore] public bool UsResPacks { get; set; } = true;
    [JsonIgnore] public VersionOntologyInfo VersionOntologyInfo { get; set; } = null!;
}