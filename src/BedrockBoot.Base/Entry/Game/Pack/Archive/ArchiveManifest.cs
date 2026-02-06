using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive;

public class ArchiveManifest
{
    [JsonPropertyName("manifest")] public Dictionary<string, List<ArchiveInfo>> Manifest { get; set; } = new();
}