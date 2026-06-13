using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game;
using Round.SDK.Global;

namespace BedrockBoot.Core.Models.Helper;

public static class SessionStoreHelper
{
    private const string SessionFileName = "sessions.json";
    private const string ConfigSubPath = "config/BedrockBoot2";

    private static string GetSessionFilePath(string versionPath)
        => Path.Combine(versionPath, ConfigSubPath, SessionFileName);

    public static List<GameSessionEntry> LoadSessions(string versionPath)
    {
        var path = GetSessionFilePath(versionPath);
        if (!File.Exists(path)) return new List<GameSessionEntry>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<GameSessionEntry>>(json, JsonSerializerOption.Options)
                   ?? new List<GameSessionEntry>();
        }
        catch
        {
            return new List<GameSessionEntry>();
        }
    }

    public static void SaveSessions(string versionPath, List<GameSessionEntry> sessions)
    {
        var path = GetSessionFilePath(versionPath);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(sessions, JsonSerializerOption.Options);
        File.WriteAllText(path, json);
    }

    public static void AddSession(string versionPath, DateTime startTime, long durationSeconds)
    {
        var sessions = LoadSessions(versionPath);
        sessions.Add(new GameSessionEntry
        {
            StartTime = startTime,
            DurationSeconds = durationSeconds
        });
        SaveSessions(versionPath, sessions);
    }

    public static Dictionary<DateTime, long> GetDailyTotals(string versionPath, int days = 7)
    {
        var sessions = LoadSessions(versionPath);
        var startDate = DateTime.Now.Date.AddDays(-(days - 1));
        var filtered = sessions.Where(s => s.StartTime >= startDate).ToList();

        var result = new Dictionary<DateTime, long>();
        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            result[date] = filtered.Where(s => s.StartTime.Date == date).Sum(s => s.DurationSeconds);
        }

        return result;
    }

    public static (int Sessions, int ActiveDays, double TotalHours) GetWeeklyStats(string versionPath)
    {
        var sessions = LoadSessions(versionPath);
        var weekAgo = DateTime.Now.Date.AddDays(-6);
        var weekly = sessions.Where(s => s.StartTime >= weekAgo).ToList();

        var totalSessions = weekly.Count;
        var activeDays = weekly.Select(s => s.StartTime.Date).Distinct().Count();
        var totalHours = weekly.Sum(s => s.DurationSeconds) / 3600.0;

        return (totalSessions, activeDays, totalHours);
    }
}
