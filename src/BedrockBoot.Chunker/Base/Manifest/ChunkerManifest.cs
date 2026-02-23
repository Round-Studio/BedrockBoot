using System.Text.Json.Serialization;

namespace BedrockBoot.Chunker.Base.Manifest;

public class ChunkerManifest
{
    [JsonPropertyName("fileName")] public string FileName { get; set; }
    [JsonPropertyName("parts")] public List<string> Parts { get; set; }
}