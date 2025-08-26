using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Models.Entry.Pack;

public class ResourcePackManifestEntry
{
    [JsonPropertyName("format_version")]
    public int FormatVersion { get; set; }

    [JsonPropertyName("header")]
    public HeaderEntry Header { get; set; } = new HeaderEntry();

    [JsonPropertyName("modules")]
    public List<ModuleEntry> Modules { get; set; } = new List<ModuleEntry>();

    [JsonPropertyName("subpacks")]
    public List<SubpackEntry> Subpacks { get; set; } = new List<SubpackEntry>();

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = new List<string>();



    public class HeaderEntry
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public List<int> Version { get; set; } = new List<int>();

        [JsonPropertyName("min_engine_version")]
        public List<int> MinEngineVersion { get; set; } = new List<int>();
    }

    public class ModuleEntry
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public List<int> Version { get; set; } = new List<int>();
    }

    public class SubpackEntry
    {
        [JsonPropertyName("folder_name")]
        public string FolderName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("memory_tier")]
        public int MemoryTier { get; set; }
    }
}