using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockBoot.GravityCone.Entry;

public class CliEvent
{
    [JsonPropertyName("event")] public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")] public JsonElement Data { get; set; }
}