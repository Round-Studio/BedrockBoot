using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Integration;

public class PackAuthor
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("links")] public List<string> Links { get; set; }
}