using System.Text.Json.Serialization;

namespace BedrockBoot.Lip.Manifest;

public class ToothFile
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }
    [JsonPropertyName("format_uuid")] public string FormatUuid { get; set; } = "";
    [JsonPropertyName("tooth")] public string Tooth { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("info")] public Info Info { get; set; } = new();
    [JsonPropertyName("variants")] public List<Variant> Variants { get; set; } = new();
}

public class Info
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; } = "";
}

public class Variant
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("platform")] public string Platform { get; set; } = "";
    [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; } = new();
    [JsonPropertyName("assets")] public List<Asset> Assets { get; set; } = new();
    [JsonPropertyName("remove_files")] public List<string> RemoveFiles { get; set; } = new();
    [JsonPropertyName("scripts")] public Scripts Scripts { get; set; } = new();
}

public class Asset
{
    [JsonPropertyName("type")] public string Type { get; set; } = "zip";
    [JsonPropertyName("urls")] public List<string>? Urls { get; set; }
    [JsonPropertyName("placements")] public List<Placement>? Placements { get; set; }
}

public class Placement
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("src")] public string Src { get; set; } = "";
    [JsonPropertyName("dest")] public string Dest { get; set; } = "";
}

public class Scripts
{
    [JsonPropertyName("post_install")] public List<string> PostInstall { get; set; } = new();
    [JsonPropertyName("post_uninstall")] public List<string> PostUninstall { get; set; } = new();
}