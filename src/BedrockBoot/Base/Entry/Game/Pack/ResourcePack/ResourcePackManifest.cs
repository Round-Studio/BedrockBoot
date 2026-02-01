using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

public class ResourcePackManifest
{
    [JsonIgnore] public string? PackRootPath { get; set; }

    [JsonIgnore]
    public string? PackIcon => File.Exists(Path.Combine(PackRootPath!, "pack_icon.png"))
        ? Path.Combine(PackRootPath!, "pack_icon.png")
        : File.Exists(Path.Combine(PackRootPath!, "pack.png"))
            ? Path.Combine(PackRootPath!, "pack.png")
            : string.Empty;

    [JsonIgnore] public ResourcePackType PackType { get; set; } = ResourcePackType.Unknown;

    [JsonPropertyName("format_version")] public object FormatVersion { get; set; }

    [JsonPropertyName("header")] public HeaderEntry Header { get; set; }

    [JsonPropertyName("modules")] public List<Module> Modules { get; set; }

    [JsonPropertyName("metadata")] public MetadataEntry Metadata { get; set; }

    [JsonPropertyName("subpacks")] public List<Subpack> Subpacks { get; set; }

    [JsonPropertyName("settings")] public List<Setting> Settings { get; set; }

    [JsonPropertyName("dependencies")] public List<Dependency> Dependencies { get; set; }

    [JsonPropertyName("capabilities")] public List<string> Capabilities { get; set; }

    // 添加一个方法来处理动态数据
    [JsonExtensionData] public Dictionary<string, JsonElement> ExtensionData { get; set; }

    // Header 类
    public class HeaderEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("uuid")] public string Uuid { get; set; }

        [JsonPropertyName("pack_scope")] public string PackScope { get; set; }

        // 使用 JsonElement 接收原始数据
        [JsonPropertyName("version")] public JsonElement VersionElement { get; set; }

        // 使用 JsonElement 接收原始数据
        [JsonPropertyName("min_engine_version")]
        public JsonElement MinEngineVersionElement { get; set; }

        [JsonPropertyName("pack_optimization_version")]
        public JsonElement PackOptimizationVersionElement { get; set; }

        // 提供便捷属性
        [JsonIgnore]
        public string Version
        {
            get
            {
                if (VersionElement.ValueKind == JsonValueKind.String)
                    return VersionElement.GetString();
                if (VersionElement.ValueKind == JsonValueKind.Array)
                {
                    var array = JsonSerializer.Deserialize<List<int>>(VersionElement.GetRawText());
                    return string.Join(".", array);
                }

                return string.Empty;
            }
        }

        [JsonIgnore]
        public string MinEngineVersion
        {
            get
            {
                if (MinEngineVersionElement.ValueKind == JsonValueKind.String)
                    return MinEngineVersionElement.GetString();
                if (MinEngineVersionElement.ValueKind == JsonValueKind.Array)
                {
                    var array = JsonSerializer.Deserialize<List<int>>(MinEngineVersionElement.GetRawText());
                    return string.Join(".", array);
                }

                return string.Empty;
            }
        }

        [JsonIgnore]
        public string PackOptimizationVersion =>
            PackOptimizationVersionElement.ValueKind == JsonValueKind.String
                ? PackOptimizationVersionElement.GetString()
                : string.Empty;
    }

    // Module 类
    public class Module
    {
        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("uuid")] public string Uuid { get; set; }

        // 使用 JsonElement 接收原始数据
        [JsonPropertyName("version")] public JsonElement VersionElement { get; set; }

        [JsonIgnore]
        public string Version
        {
            get
            {
                if (VersionElement.ValueKind == JsonValueKind.String)
                    return VersionElement.GetString();
                if (VersionElement.ValueKind == JsonValueKind.Array)
                {
                    var array = JsonSerializer.Deserialize<List<int>>(VersionElement.GetRawText());
                    return string.Join(".", array);
                }

                return string.Empty;
            }
        }

        [JsonPropertyName("language")] public string Language { get; set; }

        [JsonPropertyName("entry")] public string Entry { get; set; }
    }

    // Metadata 类
    public class MetadataEntry
    {
        [JsonPropertyName("authors")] public List<string> Authors { get; set; }

        [JsonPropertyName("license")] public string License { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    // Subpack 类
    public class Subpack
    {
        [JsonPropertyName("folder_name")] public string FolderName { get; set; }

        // 使用 JsonElement 接收两种可能的字段名
        [JsonPropertyName("memory_performance_tier")]
        public JsonElement MemoryPerformanceTierElement { get; set; }

        [JsonPropertyName("memory_tier")] public JsonElement MemoryTierElement { get; set; }

        // 统一的属性访问器
        [JsonIgnore]
        public int Tier
        {
            get
            {
                if (MemoryPerformanceTierElement.ValueKind == JsonValueKind.Number)
                    return MemoryPerformanceTierElement.GetInt32();
                if (MemoryPerformanceTierElement.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(MemoryPerformanceTierElement.GetString(), out var result))
                        return result;
                }
                else if (MemoryTierElement.ValueKind == JsonValueKind.Number)
                {
                    return MemoryTierElement.GetInt32();
                }
                else if (MemoryTierElement.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(MemoryTierElement.GetString(), out var result))
                        return result;
                }

                return 0;
            }
        }

        [JsonPropertyName("name")] public string Name { get; set; }
    }

    // Setting 类
    public class Setting
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("text")] public string Text { get; set; }
    }

    // Dependency 类
    public class Dependency
    {
        [JsonPropertyName("module_name")] public string ModuleName { get; set; }

        [JsonPropertyName("uuid")] public string Uuid { get; set; }

        // 使用 JsonElement 接收原始数据
        [JsonPropertyName("version")] public JsonElement VersionElement { get; set; }

        [JsonIgnore]
        public string Version
        {
            get
            {
                if (VersionElement.ValueKind == JsonValueKind.String)
                    return VersionElement.GetString();
                if (VersionElement.ValueKind == JsonValueKind.Array)
                {
                    var array = JsonSerializer.Deserialize<List<int>>(VersionElement.GetRawText());
                    return string.Join(".", array);
                }

                return string.Empty;
            }
        }
    }
}