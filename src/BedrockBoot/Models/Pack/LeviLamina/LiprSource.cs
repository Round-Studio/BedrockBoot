using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BedrockBoot.Models.Pack.LeviLamina
{
    public class LiprResponse
    {
        [JsonPropertyName("format_version")] public int FormatVersion { get; set; }

        [JsonPropertyName("format_uuid")] public string FormatUuid { get; set; }

        [JsonPropertyName("packages")] public Dictionary<string, PackageInfo> Packages { get; set; }
    }

    public class PackageInfo
    {
        [JsonPropertyName("stargazer_count")] public int StargazerCount { get; set; }

        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("info")] public PackageMetadata Info { get; set; }

        [JsonPropertyName("variants")] public Dictionary<string, VariantInfo> Variants { get; set; }
    }

    public class PackageMetadata
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("tags")] public List<string> Tags { get; set; }

        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
    }

    public class VariantInfo
    {
        [JsonPropertyName("versions")] public Dictionary<string, VersionInfo> Versions { get; set; }
    }

    public class VersionInfo
    {
        [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; }
    }

    public static class LiprSource
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static LiprResponse _cachedData = null;
        private static readonly object _lock = new object();

        public static async Task<LiprResponse> GetDataAsync()
        {
            if (_cachedData != null)
            {
                return _cachedData;
            }

            lock (_lock)
            {
                if (_cachedData != null)
                {
                    return Task.FromResult(_cachedData).Result;
                }
            }

            var response = await _httpClient.GetAsync("https://lipr.levimc.org/levilauncher.json");
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            };
            var data = JsonSerializer.Deserialize<LiprResponse>(json, options);

            data.Packages = data.Packages
                .Where(x => x.Value.Variants.Keys.Contains("client"))
                .ToDictionary(x => x.Key, x => x.Value);

            lock (_lock)
            {
                _cachedData = data;
            }

            return data;
        }

        public static async Task<LiprResponse> RefreshDataAsync()
        {
            lock (_lock)
            {
                _cachedData = null;
            }

            return await GetDataAsync();
        }

        public static LiprResponse GetData()
        {
            return GetDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static LiprResponse RefreshData()
        {
            return RefreshDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}