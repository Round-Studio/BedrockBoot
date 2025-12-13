using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class FunctionOptionEntry
{
    [JsonPropertyName("isImportGamePack")] public bool IsImportGamePack { get; set; }
}