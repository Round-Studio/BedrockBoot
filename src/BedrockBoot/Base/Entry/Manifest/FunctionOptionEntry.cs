using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Manifest;

public class FunctionOptionEntry
{
    [JsonPropertyName("isEnableImportGamePack")] public bool IsEnableImportGamePack { get; set; }
    [JsonPropertyName("isEnableGameInstanceControl")] public bool IsEnableGameInstanceControl { get; set; }
    [JsonPropertyName("isEnableGameInstanceMods")] public bool IsEnableGameInstanceMods { get; set; }
    [JsonPropertyName("isEnableWebProtocol")] public bool IsEnableWebProtocol { get; set; }
    [JsonPropertyName("isEnableSettingPersonalization")] public bool IsEnableSettingPersonalization { get; set; }
    [JsonPropertyName("isEnableSettingBackground")] public bool IsEnableSettingBackground { get; set; }
    [JsonPropertyName("isEnableSettingColor")] public bool IsEnableSettingColor { get; set; }
    [JsonPropertyName("isEnablePlugin")] public bool IsEnablePlugin { get; set; }
}