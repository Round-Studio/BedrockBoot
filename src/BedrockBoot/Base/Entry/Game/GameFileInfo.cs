using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game;

public class GameFileInfo
{
    [JsonPropertyName("filePath")] public string FilePath { get; set; }
    [JsonPropertyName("hash")] public string Hash { get; set; }
}