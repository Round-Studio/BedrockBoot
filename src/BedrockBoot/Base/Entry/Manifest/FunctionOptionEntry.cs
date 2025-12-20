using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class FunctionOptionEntry
{
    [JsonPropertyName("isImportGamePack")] public bool IsImportGamePack { get; set; }
    [JsonPropertyName("isGameInstanceControl")] public bool IsGameInstanceControl { get; set; }
    [JsonPropertyName("isEnableWebProtocol")] public bool IsEnableWebProtocol { get; set; }
}