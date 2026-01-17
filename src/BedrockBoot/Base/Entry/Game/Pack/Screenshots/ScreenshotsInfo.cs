using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Screenshots;

public class ScreenshotsInfo
{
    [JsonPropertyName("captureTime")] public ulong CaptureTime { get; set; }
    [JsonIgnore] public string FilePath { get; set; }
}