using System;
using System.Text.Json.Serialization;
using BedrockLauncher.Core;

namespace BedrockBoot.Base.Entry.Game;

public class VersionConfig
{
    [JsonPropertyName("info")] public VersionInfo Info { get; set; }
    [JsonPropertyName("config")] public VersionConfigEntry Config { get; set; } = new();
    [JsonPropertyName("playerData")] public PlayerDataEntry PlayerData { get; set; } = new();
    [JsonPropertyName("gameStatus")] public VersionStatusEntry VersionStatus { get; set; } = new();

    [JsonIgnore] public string VersionPath { get; set; }
    [JsonIgnore] public string VersionsRootPath { get; set; }
    [JsonIgnore] public string BodyFile { get; set; }

    public class VersionInfo
    {
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("buildType")] public MinecraftBuildTypeVersion BuildType { get; set; }
        [JsonPropertyName("versionName")] public string VersionName { get; set; }
        [JsonPropertyName("versionType")] public MinecraftGameTypeVersion VersionType { get; set; }
        [JsonPropertyName("coverImage")] public string? CoverImage { get; set; } = null;
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
    }
    
    // 新增：玩家数据类
    public class PlayerDataEntry
    {
        [JsonPropertyName("totalPlayTime")] public long TotalPlayTime { get; set; }
        [JsonPropertyName("lastPlayTime")] public DateTime? LastPlayTime { get; set; }
        [JsonPropertyName("totalSessions")] public int TotalSessions { get; set; }
        [JsonPropertyName("firstPlayTime")] public DateTime? FirstPlayTime { get; set; }
        
        [JsonIgnore]
        public string FormattedTotalPlayTime
        {
            get
            {
                TimeSpan ts = TimeSpan.FromSeconds(TotalPlayTime);
                return $"{(int)ts.TotalHours}小时{ts.Minutes}分钟{ts.Seconds}秒";
            }
        }
    }
}