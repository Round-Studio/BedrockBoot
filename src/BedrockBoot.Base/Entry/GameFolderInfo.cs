using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Base.Entry;

public class GameFolderInfo
{
    [JsonPropertyName("gameFolderPath")] public string GameFolderPath { get; set; }
    [JsonPropertyName("gameFolderName")] public string GameFolderName { get; set; }
    [JsonPropertyName("gameSelIndex")] public int GameSelIndex { get; set; } = 0;
    [JsonPropertyName("gameFolderType")] public GameFolderType GameFolderType { get; set; } = GameFolderType.BedrockBoot;
    [JsonPropertyName("gameFolderFilter")] public GameFolderFilterType GameFolderFilter { get; set; } = GameFolderFilterType.AllTypes;
}