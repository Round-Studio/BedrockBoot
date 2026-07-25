using System.Text.Json.Serialization;

namespace BedrockBoot.GravityCone.Entry;

public class CliRequest
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")] public object Params { get; set; } = new { };
}