using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

public class ResourcePackManifest
{
    [JsonIgnore]
    public string? PackRootPath { get; set; }
    
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; set; }
        
    [JsonPropertyName("header")]
    public HeaderEntry Header { get; set; }
        
    [JsonPropertyName("modules")]
    public List<Module> Modules { get; set; }
        
    [JsonPropertyName("dependencies")]
    public List<Dependency> Dependencies { get; set; }
        
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; }

    // Header 类
    public class HeaderEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }
        
        [JsonPropertyName("version")]
        public List<int> Version { get; set; }
        
        [JsonPropertyName("min_engine_version")]
        public List<int> MinEngineVersion { get; set; }
    }

    // Module 类
    public class Module
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }
        
        [JsonPropertyName("version")]
        public List<int> Version { get; set; }
        
        [JsonPropertyName("language")]
        public string Language { get; set; }
        
        [JsonPropertyName("entry")]
        public string Entry { get; set; }
    }

    // Dependency 类
    public class Dependency
    {
        [JsonPropertyName("module_name")]
        public string ModuleName { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}