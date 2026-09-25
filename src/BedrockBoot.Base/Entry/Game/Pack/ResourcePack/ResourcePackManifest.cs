/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;
using Round.SDK.Entity;

namespace BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

public class ResourcePackManifest
{
    [JsonIgnore] public string? PackRootPath { get; set; }
    [JsonIgnore] public byte[]? PackIconBytes { get; set; }
    [JsonIgnore] public string? PackIcon => PackIconBytes != null
        ? "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png"
        : File.Exists(Path.Combine(PackRootPath!, "pack_icon.png"))
            ? Path.Combine(PackRootPath!, "pack_icon.png")
            : File.Exists(Path.Combine(PackRootPath!, "pack.png"))
                ? Path.Combine(PackRootPath!, "pack.png")
                : File.Exists(Path.Combine(PackRootPath!, "world_icon.jpeg"))
                    ? Path.Combine(PackRootPath!, "world_icon.jpeg")
                    : "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png";
    [JsonIgnore] public ResourcePackType PackType { get; set; } = ResourcePackType.Unknown;
    public void SaveConfig()
    {
        var filePath = Path.Combine(PackRootPath, "manifest.json");
        var conf = new ConfigEntity<ResourcePackManifest>(filePath);
        conf.Data = this;
        conf.Save();
    }
    [JsonPropertyName("format_version")] public object FormatVersion { get; set; }
    [JsonPropertyName("header")] public HeaderEntry Header { get; set; }
    [JsonPropertyName("modules")] public List<Module> Modules { get; set; }
    [JsonPropertyName("metadata")] public MetadataEntry Metadata { get; set; }
    [JsonPropertyName("subpacks")] public List<Subpack> Subpacks { get; set; }
    [JsonPropertyName("settings")] public List<Setting> Settings { get; set; }
    [JsonPropertyName("dependencies")] public List<Dependency> Dependencies { get; set; }
    [JsonPropertyName("capabilities")] public List<string> Capabilities { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement> ExtensionData { get; set; }
    public class HeaderEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("pack_id")] public string PackId { get; set; }
        [JsonPropertyName("packs_version")] public string PacksVersion { get; set; }
        private string _uuid;
        [JsonPropertyName("uuid")]
        public string Uuid
        {
            get => !string.IsNullOrEmpty(_uuid) ? _uuid : PackId;
            set => _uuid = value;
        }
        [JsonPropertyName("pack_scope")] public string PackScope { get; set; }
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement VersionElement { get; set; }
        [JsonPropertyName("min_engine_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement MinEngineVersionElement { get; set; }
        [JsonPropertyName("pack_optimization_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement PackOptimizationVersionElement { get; set; }
        [JsonIgnore] public string Version
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
                if (!string.IsNullOrEmpty(PacksVersion))
                    return PacksVersion;
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
        [JsonIgnore] public string PackOptimizationVersion =>
            PackOptimizationVersionElement.ValueKind == JsonValueKind.String
                ? PackOptimizationVersionElement.GetString()
                : string.Empty;
    }
    public class Module
    {
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement VersionElement { get; set; }
        [JsonIgnore] public string Version
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
    public class MetadataEntry
    {
        [JsonPropertyName("authors")] public List<string> Authors { get; set; }
        [JsonPropertyName("license")] public string License { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("product_type")] public string ProductType { get; set; }
    }
    public class Subpack
    {
        [JsonPropertyName("folder_name")] public string FolderName { get; set; }
        [JsonPropertyName("memory_performance_tier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement MemoryPerformanceTierElement { get; set; }
        [JsonPropertyName("memory_tier")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement MemoryTierElement { get; set; }
        [JsonIgnore] public int Tier
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
    public class Setting
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
    }
    public class Dependency
    {
        [JsonPropertyName("module_name")] public string ModuleName { get; set; }
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonElement VersionElement { get; set; }
        [JsonIgnore] public string Version
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