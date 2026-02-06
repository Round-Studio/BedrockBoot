using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry;

public class WindowInfo
{
    [JsonPropertyName("x")] public int X { get; set; } = -1;
    [JsonPropertyName("y")] public int Y { get; set; } = -1;
    [JsonPropertyName("width")] public double Width { get; set; } = 900;
    [JsonPropertyName("height")] public double Height { get; set; } = 520;
}