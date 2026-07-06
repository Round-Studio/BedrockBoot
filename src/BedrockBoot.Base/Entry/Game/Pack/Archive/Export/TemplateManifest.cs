namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Export;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TemplateManifest
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; } = 2;
    [JsonPropertyName("header")] public HeaderEntry Header { get; set; }
    [JsonPropertyName("modules")] public List<ModuleEntry> Modules { get; set; }

    public class HeaderEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("base_game_version")] public List<int> BaseGameVersion { get; set; }
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")] public List<int> Version { get; set; }
        [JsonPropertyName("allow_random_seed")] public bool AllowRandomSeed { get; set; }
        [JsonPropertyName("lock_template_options")] public bool LockTemplateOptions { get; set; }
    }

    public class ModuleEntry
    {
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; set; } = "world_template";
        [JsonPropertyName("uuid")] public string Uuid { get; set; }
        [JsonPropertyName("version")] public List<int> Version { get; set; }
    }
}