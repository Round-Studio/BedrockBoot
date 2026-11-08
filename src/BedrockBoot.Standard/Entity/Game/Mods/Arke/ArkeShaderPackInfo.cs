using System.Text.Json.Serialization;

namespace BedrockBoot.Standard.Entity.Game.Mods.Arke;

public class ArkeShaderPackInfo
{
    [JsonPropertyName("name")] public string PackName { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string PackPath { get; set; } = string.Empty;
    [JsonPropertyName("size")] public long PackSize { get; set; } = 0;
}