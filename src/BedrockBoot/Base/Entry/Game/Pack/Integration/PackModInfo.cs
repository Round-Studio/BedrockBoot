using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Integration;

public class PackModInfo
{
    [JsonPropertyName("isPreLoad")] public bool IsPreLoad { get; set; } = false;
    [JsonPropertyName("delay")] public int Delay { get; set; } = 0;
}