/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Platform;

namespace BedrockBoot.Models.Helper;

public static class ChangeLogLoader
{
    private const string EmbeddedUri = "avares://BedrockBoot/Assets/Text/ChangLog.txt";

    public static IReadOnlyList<ChangeLogEntry> Load()
    {
        var result = new List<ChangeLogEntry>();

        try
        {
            // 优先读取内嵌资源：单文件发布(PublishSingleFile)不会携带松散文件，
            // 且内嵌发生在编译期，早于 PostBuild 把源文件还原为占位符
            var text = ReadEmbeddedText();

            if (text == null)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Text", "ChangLog.txt");
                if (File.Exists(path)) text = File.ReadAllText(path);
            }

            if (text == null) return result;

            foreach (var line in text.Split('\n'))
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

    private static string ReadEmbeddedText()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(EmbeddedUri));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception)
        {
            return null;
        }
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