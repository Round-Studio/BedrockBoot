using System.Text.Json.Serialization;

namespace BedrockBoot.GravityCone.Entry.Result;

public class LanServer
{
    [JsonPropertyName("motd")] public string Motd { get; set; } = string.Empty;

    [JsonPropertyName("ip")] public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("port")] public int Port { get; set; }
}