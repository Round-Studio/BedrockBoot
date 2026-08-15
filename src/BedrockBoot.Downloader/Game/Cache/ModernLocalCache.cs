using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using SourceList = BedrockBoot.Downloader.Global.SourceList;

namespace BedrockBoot.Downloader.Game.Cache;

public class ModernLocalCache
{
    public class FileCacheInfo
    {
        [JsonPropertyName("pathname")] public string Pathname { get; set; } = string.Empty;
        [JsonPropertyName("hashes")] public HashesInfo Hashes { get; set; } = new();
        [JsonPropertyName("size")] public long Size { get; set; }

        [JsonPropertyName("localFile")] public string LocalFile { get; set; } = string.Empty;
        [JsonPropertyName("isLocalFile")] public bool IsLocalFile { get; set; } = false;

        public class HashesInfo
        {
            [JsonPropertyName("sha1")] public string Sha1 { get; set; } = string.Empty;
            [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
        }
    }
    
    public class VersionManifest
    {
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("files")] public List<FileCacheInfo> Files { get; set; } = new();
    }

    public static ConfigEntity<List<FileCacheInfo>> Caches { get; private set; } =
        new(Path.Combine(PathsList.ConfigFolderPath, "ModernFileCacheIndex.json"));

    public static async Task<List<FileCacheInfo>> GetVersionFilesAsync(string version)
    {
        Caches.Load();
        var url = $"{SourceList.BaseUrl}/versions/{version}";
        
        Console.WriteLine(url);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", GameDownloader.UserAgent);

        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var result = JsonSerializer.Deserialize<VersionManifest>(jsonContent, options) ??
                         new VersionManifest();

            var cachedItems = Caches.Data;

            if (cachedItems != null && cachedItems.Count > 0)
            {
                var cacheDict = cachedItems
                    .Where(c => c.Hashes != null && !string.IsNullOrEmpty(c.Hashes.Sha256))
                    .GroupBy(c => c.Hashes.Sha256)
                    .ToDictionary(g => g.Key, g => g.First());

                bool hasCacheChanged = false;

                for (int i = 0; i < result.Files.Count; i++)
                {
                    var item = result.Files[i];
                    if (item.Hashes != null && !string.IsNullOrEmpty(item.Hashes.Sha256))
                    {
                        if (cacheDict.TryGetValue(item.Hashes.Sha256, out var matchedCache))
                        {
                            if (File.Exists(matchedCache.LocalFile))
                            {
                                matchedCache.IsLocalFile = true;
                                result.Files[i] = matchedCache;
                            }
                            else
                            {
                                cachedItems.Remove(matchedCache);
                                hasCacheChanged = true;
                            }
                        }
                    }
                }

                if (hasCacheChanged)
                {
                    Caches.Save();
                }
            }

            return result.Files;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON deserialization failed: {ex.Message}");
            throw;
        }
    }
}