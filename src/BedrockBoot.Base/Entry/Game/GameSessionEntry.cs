using System;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game;

public class GameSessionEntry
{
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("durationSeconds")]
    public long DurationSeconds { get; set; }
}
