using System.Collections.Generic;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Language;

namespace BedrockBoot.Base.Entry;

public class ConfigEntry
{
    [JsonPropertyName("windowInfo")] public WindowInfo WindowInfo { get; set; } = new();
    [JsonPropertyName("gameFolders")] public List<GameFolderInfo> GameFolders { get; set; } = new();
    [JsonPropertyName("gameFolderSelIndex")] public int GameFolderSelIndex { get; set; } = -1;
    [JsonPropertyName("downloadChunkCount")] public int DownloadChunkCount { get; set; } = 4;
    [JsonPropertyName("versionSourceIndex")] public int VersionSourceIndex { get; set; } = 0;
    [JsonPropertyName("curseForgeSourceIndex")] public int CurseForgeSourceIndex { get; set; } = 0;
    [JsonPropertyName("styleConfig")] public StyleConfig StyleConfig { get; set; } = new();
    [JsonPropertyName("homeConfig")] public HomeConfig HomeConfig { get; set; } = new();
    [JsonPropertyName("isAutoCacheGamePack")] public bool IsAutoCacheGamePack { get; set; } = true;
    [JsonPropertyName("isAutoCheckUpdate")] public bool IsAutoCheckUpdate { get; set; } = true;
    [JsonPropertyName("isFirstRun")] public bool IsFirstRun { get; set; } = true;
    [JsonPropertyName("isAgreeTerms")] public bool IsAgreeTerms { get; set; } = false;
    [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
    [JsonPropertyName("isTaskBarJumpItem")] public bool IsTaskBarJumpItem { get; set; } = true;
    [JsonPropertyName("updateType")] public UpdateType UpdateType { get; set; } = UpdateType.Release;
    [JsonPropertyName("isolationModel")] public IsolationType IsolationModel { get; set; } = IsolationType.Hook;
    [JsonPropertyName("language")] public LanguageEnum Language { get; set; } = LanguageEnum.Chinese;
    [JsonPropertyName("gatherInfo")] public bool GatherInfo { get; set; } = true;
    [JsonPropertyName("launchBehavior")] public LaunchBehaviorEnum LaunchBehavior { get; set; } = LaunchBehaviorEnum.Normal;
}

public class StyleConfig
{
    [JsonPropertyName("lightThemeType")] public ThemeModelEnum LightThemeType { get; set; } = ThemeModelEnum.Dark;
    [JsonPropertyName("backgroundImages")] public List<string> BackgroundImages { get; set; } = new();
    [JsonPropertyName("backgroundImageSelectedIndex")] public int BackgroundImageSelectedIndex { get; set; } = -1;
    [JsonPropertyName("backgroundImageOpacity")] public int BackgroundImageOpacity { get; set; } = 100;
    [JsonPropertyName("backgroundImageBlur")] public int BackgroundImageBlur { get; set; } = 1;
    [JsonPropertyName("styleType")] public StyleType StyleType { get; set; } = StyleType.Voronoi;
    [JsonPropertyName("accentColorIndex")] public int AccentColorIndex { get; set; } = 36;
    [JsonPropertyName("background3d")] public bool Background3D { get; set; } = false;
}

public class HomeConfig
{
    [JsonPropertyName("homeType")] public HomeType HomeType { get; set; } = HomeType.None;
    [JsonPropertyName("homeXmlFiles")] public List<string> HomeXmlFiles { get; set; } = new();
    [JsonPropertyName("homeXmlSelIndex")] public int HomeXmlSelIndex { get; set; } = -1;
}