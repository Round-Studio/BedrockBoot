using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.LeviLamina.Base.Entry.Manifest;

public class ToothManifest
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }

    [JsonPropertyName("format_uuid")] public string FormatUuid { get; set; }

    [JsonPropertyName("tooth")] public string Tooth { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("info")] public InfoEntry Info { get; set; }

    [JsonPropertyName("variants")] public List<VariantEntry> Variants { get; set; }

    public class InfoEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("tags")] public List<string> Tags { get; set; }

        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
    }

    public class VariantEntry
    {
        [JsonPropertyName("label")] public string Label { get; set; }

        [JsonPropertyName("platform")] public string Platform { get; set; }

        [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; }

        [JsonPropertyName("assets")] public List<AssetEntry> Assets { get; set; }

        [JsonPropertyName("remove_files")] public List<string> RemoveFiles { get; set; }

        [JsonPropertyName("scripts")] public ScriptsEntry Scripts { get; set; }
    }

    public class AssetEntry
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("urls")] public List<string> Urls { get; set; }

        [JsonPropertyName("placements")] public List<PlacementEntry> Placements { get; set; }
    }

    public class PlacementEntry
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("src")] public string Src { get; set; }

        [JsonPropertyName("dest")] public string Dest { get; set; }
    }

    public class ScriptsEntry
    {
        [JsonPropertyName("post_install")] public List<string> PostInstall { get; set; }

        [JsonPropertyName("post_uninstall")] public List<string> PostUninstall { get; set; }
    }
}