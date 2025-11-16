using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry;

public class GameFolderInfo
{
    [JsonPropertyName("gameFolderPath")] 
    public string GameFolderPath { get; set; }
    [JsonPropertyName("gameFolderName")]
    public string GameFolderName { get; set; }
}