using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.Loaders.LeviLamina;

public class LeviLaminaVersionDb
{
    [JsonPropertyName("format_version")] public int FormatVersion { get; set; }

    [JsonPropertyName("versions")] public Dictionary<string, List<string>> Versions { get; set; }

    private static readonly HttpClient _httpClient = new HttpClient();
    private static LeviLaminaVersionDb _cache;
    private static DateTime _cacheTime;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
    private static readonly object _lock = new object();

    public static async Task<LeviLaminaVersionDb> FetchAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && IsCacheValid())
        {
            return _cache;
        }

        try
        {
            string json = await _httpClient.GetStringAsync(SourceList.LeviLaminaVersionDb);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<LeviLaminaVersionDb>(json, options);

            lock (_lock)
            {
                _cache = result;
                _cacheTime = DateTime.UtcNow;
            }

            return result;
        }
        catch
        {
            return _cache;
        }
    }

    public static LeviLaminaVersionDb Fetch(bool forceRefresh = false)
    {
        return FetchAsync(forceRefresh).GetAwaiter().GetResult();
    }

    private static bool IsCacheValid()
    {
        lock (_lock)
        {
            return _cache != null && DateTime.UtcNow - _cacheTime < _cacheDuration;
        }
    }

    public static void ClearCache()
    {
        lock (_lock)
        {
            _cache = null;
            _cacheTime = DateTime.MinValue;
        }
    }

    public List<string> GetLeviLaminaVersions(string bedrockVersion)
    {
        if (Versions == null || string.IsNullOrEmpty(bedrockVersion))
            return new List<string>();

        return Versions.TryGetValue(bedrockVersion, out var versions) ? versions : new List<string>();
    }

    public string GetLatestLeviLaminaVersion(string bedrockVersion)
    {
        var versions = GetLeviLaminaVersions(bedrockVersion);
        return versions.Count > 0 ? versions[versions.Count - 1] : null;
    }

    public bool IsVersionCompatible(string bedrockVersion, string leviLaminaVersion)
    {
        var versions = GetLeviLaminaVersions(bedrockVersion);
        return versions.Contains(leviLaminaVersion);
    }
}