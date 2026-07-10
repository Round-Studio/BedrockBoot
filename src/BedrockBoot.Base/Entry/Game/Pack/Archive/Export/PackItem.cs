using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Export;

public class PackItem
{
    [JsonPropertyName("pack_id")] public string PackId { get; set; }
    [JsonPropertyName("version")] public List<int> Version { get; set; }
}