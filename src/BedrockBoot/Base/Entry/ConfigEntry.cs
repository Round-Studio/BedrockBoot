using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry;

public class ConfigEntry
{
    [JsonPropertyName("gameFolders")] public List<GameFolderInfo> GameFolders { get; set; } = new();

    [JsonPropertyName("gameFolderSelIndex")] public int GameFolderSelIndex { get; set; } = -1;
}