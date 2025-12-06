using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry;

public class ConfigEntry
{
    [JsonPropertyName("windowInfo")] public WindowInfo WindowInfo { get; set; } = new WindowInfo();
    [JsonPropertyName("gameFolders")] public List<GameFolderInfo> GameFolders { get; set; } = new();
    [JsonPropertyName("gameFolderSelIndex")] public int GameFolderSelIndex { get; set; } = -1;
    [JsonPropertyName("isAutoCacheGamePack")] public bool IsAutoCacheGamePack { get; set; } = true;
    [JsonPropertyName("isFirstRun")] public bool IsFirstRun { get; set; } = true;
    [JsonPropertyName("isAgreeTerms")] public bool IsAgreeTerms { get; set; } = false;
}