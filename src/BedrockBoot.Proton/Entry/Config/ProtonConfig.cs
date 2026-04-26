using System.Text.Json.Serialization;

namespace BedrockBoot.Proton.Entry.Config;

public class ProtonConfig
{
    [JsonPropertyName("selectProtonPath")] public string SelectProtonPath { get; set; } = string.Empty;
    [JsonPropertyName("maxProtonMemory")] public int MaxProtonMemory { get; set; } = 1024;
}