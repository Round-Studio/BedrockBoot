using System.Collections.Generic;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Entry;

public class ConfigEntry
{
    [JsonPropertyName("windowInfo")] public WindowInfo WindowInfo { get; set; } = new WindowInfo();
    [JsonPropertyName("gameFolders")] public List<GameFolderInfo> GameFolders { get; set; } = new();
    [JsonPropertyName("gameFolderSelIndex")] public int GameFolderSelIndex { get; set; } = -1;
    [JsonPropertyName("downloadChunkCount")] public int DownloadChunkCount { get; set; } = 4;
    [JsonPropertyName("versionSourceIndex")] public int VersionSourceIndex { get; set; } = 1;
    [JsonPropertyName("styleConfig")] public StyleConfig StyleConfig { get; set; } = new();
    [JsonPropertyName("isAutoCacheGamePack")] public bool IsAutoCacheGamePack { get; set; } = true;
    [JsonPropertyName("isAutoCheckUpdate")] public bool IsAutoCheckUpdate { get; set; } = true;
    [JsonPropertyName("isFirstRun")] public bool IsFirstRun { get; set; } = true;
    [JsonPropertyName("isAgreeTerms")] public bool IsAgreeTerms { get; set; } = false;
    [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
}
public class StyleConfig
{
    [JsonPropertyName("lightThemeType")] public ThemeModelEnum LightThemeType { get; set; } = ThemeModelEnum.Dark;
    [JsonPropertyName("backgroundImages")] public List<string> BackgroundImages { get; set; } = new List<string>();
    [JsonPropertyName("backgroundImageSelectedIndex")] public int BackgroundImageSelectedIndex { get; set; } = -1;
    [JsonPropertyName("backgroundImageOpacity")] public int BackgroundImageOpacity { get; set; } = 100;
    [JsonPropertyName("backgroundImageBlur")] public int BackgroundImageBlur { get; set; } = 1;
    [JsonPropertyName("styleType")] public StyleType StyleType { get; set; } = StyleType.AccentColor;
    [JsonPropertyName("accentColorIndex")] public int AccentColorIndex { get; set; } = 36;
}