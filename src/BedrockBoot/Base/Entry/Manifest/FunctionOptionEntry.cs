using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class FunctionOptionEntry
{
    [JsonPropertyName("isEnableImportGamePack")] public bool IsEnableImportGamePack { get; set; }
    [JsonPropertyName("isEnableGameInstanceControl")] public bool IsEnableGameInstanceControl { get; set; }
    [JsonPropertyName("isEnableWebProtocol")] public bool IsEnableWebProtocol { get; set; }
    [JsonPropertyName("isEnableInstanceIndependent")] public bool IsEnableInstanceIndependent { get; set; }
    [JsonPropertyName("isEnableSettingPersonalization")] public bool IsEnableSettingPersonalization { get; set; }
}