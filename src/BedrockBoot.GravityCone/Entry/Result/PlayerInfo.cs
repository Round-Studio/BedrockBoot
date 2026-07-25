using System.Text.Json.Serialization;

namespace BedrockBoot.GravityCone.Entry.Result;

public class PlayerInfo
{
    [JsonPropertyName("player")] public string Player { get; set; } = string.Empty;

    [JsonPropertyName("clientId")] public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("isRoomHost")] public bool IsRoomHost { get; set; }
}