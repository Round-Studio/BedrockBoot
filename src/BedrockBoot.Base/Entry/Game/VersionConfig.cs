using System;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Base.Helper;
using BedrockLauncher.Core;

namespace BedrockBoot.Base.Entry.Game;

public class VersionConfig
{
    [JsonPropertyName("info")] public VersionInfo Info { get; set; }
    [JsonPropertyName("config")] public VersionConfigEntry Config { get; set; } = new();
    [JsonPropertyName("playerData")] public PlayerDataEntry PlayerData { get; set; } = new();
    [JsonPropertyName("gameStatus")] public VersionStatusEntry VersionStatus { get; set; } = new();

    [JsonIgnore] public string? VersionPath { get; set; } = string.Empty;
    [JsonIgnore] public string? VersionsRootPath { get; set; } = string.Empty;
    [JsonIgnore] public string? BodyFile { get; set; } = string.Empty;

    public class VersionInfo
    {
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("buildType")] public MinecraftBuildTypeVersion BuildType { get; set; }
        [JsonPropertyName("versionName")] public string VersionName { get; set; }
        [JsonPropertyName("versionType")] public MinecraftGameTypeVersion VersionType { get; set; }
        [JsonPropertyName("coverImage")] public string? CoverImage { get; set; } = null;
        [JsonPropertyName("gameIconType")] public GameIconType GameIconType { get; set; } = GameIconType.Default;
        [JsonPropertyName("gameIconPath")] public string GameIconPath { get; set; } = string.Empty;
    }

    public class VersionStatusEntry
    {
        [JsonPropertyName("gameInputInstalled")] public bool GameInputInstalled { get; set; } = false;
    }

    public class VersionConfigEntry
    {
        [JsonPropertyName("isEditModel")] public bool IsEditModel { get; set; } = false;
        [JsonPropertyName("isModes")] public bool IsModes { get; set; } = true;
        [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
        [JsonPropertyName("isVersionIsolated")] public bool IsVersionIsolated { get; set; } = true;
        [JsonPropertyName("isDetailedLog")] public bool IsDetailedLog { get; set; } = false;
        [JsonPropertyName("otherCommand")] public string OtherCommand { get; set; } = "";
        [JsonPropertyName("folderPolicy")] public CatalogStrategyEnum IsolationFolderPolicy { get; set; } = CatalogStrategyEnum.FollowTheBigPicture;
        [JsonPropertyName("folderPolicyString")] public string FolderPolicyStr { get; set; } = IsolationPolicyHelper.ParsePolicyConfig(CatalogStrategyEnum.Independence);    }
    
    public class PlayerDataEntry
    {
        [JsonPropertyName("totalPlayTime")] public long TotalPlayTime { get; set; }
        [JsonPropertyName("lastPlayTime")] public DateTime? LastPlayTime { get; set; }
        [JsonPropertyName("totalSessions")] public int TotalSessions { get; set; }
        [JsonPropertyName("firstPlayTime")] public DateTime? FirstPlayTime { get; set; }
    }
}