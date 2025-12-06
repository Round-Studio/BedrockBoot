using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Base.JsonContext;
using BedrockLauncher.Core;

namespace BedrockBoot.Base.Entry.Game;

public class VersionConfig
{
    [JsonPropertyName("info")] public VersionInfo Info { get; set; }
    [JsonPropertyName("config")] public VersionConfigEntry Config { get; set; } = new ();
        
    [JsonIgnore] public string VersionPath { get; set; }
    
    public class VersionInfo
    {
        [JsonPropertyName("version")] 
        public string Version { get; set; }
    
        [JsonPropertyName("buildType")]
        [JsonConverter(typeof(GameBuildTypeJsonConverter))]
        public GameBuildType BuildType { get; set; }
    
        [JsonPropertyName("versionName")]
        public string VersionName { get; set; }
    
        [JsonPropertyName("versionType")]
        [JsonConverter(typeof(GameVersionTypeJsonConverter))]
        public VersionType VersionType { get; set; }
    }
    public class VersionConfigEntry
    {
        [JsonPropertyName("isEditModel")] public bool IsEditModel { get; set; } = false;
        [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
        [JsonPropertyName("isVersionIsolated")] public bool IsVersionIsolated { get; set; } = true;
        [JsonPropertyName("otherCommand")] public string OtherCommand { get; set; } = "";
    }
}