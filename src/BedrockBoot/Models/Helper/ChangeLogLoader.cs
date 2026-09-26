using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;

namespace BedrockBoot.Models.Helper;

public static class ChangeLogLoader
{
    public static IReadOnlyList<ChangeLogEntry> Load()
    {
        var result = new List<ChangeLogEntry>();

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Text", "ChangLog.txt");
            if (!File.Exists(path)) return result;

            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = JsonSerializer.Deserialize<ChangeLogEntry>(line);
                    if (entry != null && entry.Author != "RoundStudio Bot") result.Add(entry);
                }
                catch (JsonException)
                {
                }
            }
        }
        catch (Exception)
        {
        }

        return result;
    }
}
public sealed class ChangeLogEntry
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";

    public string ShortHash
    {
        get
        {
            if (string.IsNullOrEmpty(Hash)) return "";
            return Hash.Length > 8 ? Hash.Substring(0, 8) : Hash;
        }
    }

    public string DisplayDate
    {
        get
        {
            if (System.DateTimeOffset.TryParse(Date, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return Date;
        }
    }
}