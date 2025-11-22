using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Game;

namespace BedrockBoot.Base.Entry.Game;

public class VersionConfig
{
    [JsonPropertyName("info")] public VersionInfo Info { get; set; }
    [JsonPropertyName("Config")] public VersionConfigEntry Config { get; set; } = new ();
        
    [JsonIgnore] public string VersionPath { get; set; }
    
    public class VersionInfo
    {
        [JsonPropertyName("version")] 
        public string Version { get; set; }
    
        [JsonPropertyName("buildType")]
        public GameBuildType BuildType { get; set; }
    
        [JsonPropertyName("versionName")]
        public string VersionName { get; set; }
    
        [JsonPropertyName("versionType")]
        public GameVersionType VersionType { get; set; }
    }
    public class VersionConfigEntry
    {
        [JsonPropertyName("isEditModel")] public bool IsEditModel { get; set; } = false;
        [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
        [JsonPropertyName("isVersionIsolated")] public bool IsVersionIsolated { get; set; } = true;
    }
}