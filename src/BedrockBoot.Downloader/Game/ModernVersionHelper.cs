using System.Text.Json;
using BedrockBoot.Downloader.Manifest.Game;
using BedrockBoot.Models.Global;
using SourceList = BedrockBoot.Downloader.Global.SourceList;

namespace BedrockBoot.Downloader.Game;

partial class ModernVersionHelper
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static ModernManifest _manifest = null;
    private static readonly string CacheFilePath = Path.Combine(PathsList.TempPath, "modern_version_cache.json");
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);
    private static readonly int MaxRetryCount = 3;

    public static string BaseUrl => $"{SourceList.BaseUrl}/versions";

    static ModernVersionHelper()
    {
        var osVersion = Environment.OSVersion.VersionString;
        Console.WriteLine($@"OS Version: {osVersion}");

        httpClient.DefaultRequestHeaders.Add("User-Agent", $"{GameDownloader.UserAgent} ({osVersion})");
    }

    private class ManifestCache
    {
        public DateTime CacheTime { get; set; }
        public ModernManifest Manifest { get; set; }
    }

    public static ModernManifest Manifest => _manifest;

    public static ModernManifest RefreshManifest()
    {
        _manifest = null;
        return GetManifest(forceRefresh: true);
    }

    public static void ClearCache()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                File.Delete(CacheFilePath);
                Console.WriteLine($@"缓存文件已删除: {CacheFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"删除缓存文件失败: {ex.Message}");
        }
    }

    public static TimeSpan? GetCacheAge()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                var cacheTime = File.GetLastWriteTime(CacheFilePath);
                return DateTime.Now - cacheTime;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"获取缓存年龄失败: {ex.Message}");
        }
        return null;
    }

    public static ModernManifest GetManifest(bool forceRefresh = false)
    {
        if (!forceRefresh && _manifest != null) return _manifest;

        if (!forceRefresh && TryLoadFromCache(out var cachedManifest, ignoreExpiry: false))
        {
            _manifest = cachedManifest;
            return _manifest;
        }

        var manifest = FetchManifestWithRetry();
        if (manifest != null)
        {
            _manifest = manifest;
            SaveToCache(manifest);
            return _manifest;
        }

        if (TryLoadFromCache(out var fallbackManifest, ignoreExpiry: true))
        {
            Console.WriteLine(@"网络获取失败，使用本地缓存数据（可能已过期）");
            _manifest = fallbackManifest;
            return _manifest;
        }

        Console.WriteLine(@"无法获取 Manifest：网络请求失败且无本地缓存");
        _manifest = null;
        return _manifest;
    }

    private static ModernManifest FetchManifestWithRetry()
    {
        for (int retry = 0; retry < MaxRetryCount; retry++)
        {
            try
            {
                if (retry > 0)
                {
                    Console.WriteLine($@"第 {retry + 1} 次重试获取 ModernManifest...");
                    System.Threading.Thread.Sleep(1000 * retry);
                }

                Console.WriteLine($@"从网络获取 ModernManifest: {BaseUrl}");
                var response = httpClient.GetStringAsync(BaseUrl).Result;

                var manifest = JsonSerializer.Deserialize<ModernManifest>(response);
                if (manifest != null)
                {
                    Console.WriteLine(@"成功获取 ModernManifest");
                    return manifest;
                }

                Console.WriteLine(@"获取到的 ModernManifest 为空");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"从 {BaseUrl} 获取 ModernManifest 失败 (尝试 {retry + 1}/{MaxRetryCount}): {ex.Message}");

                if (retry == MaxRetryCount - 1)
                {
                    Console.WriteLine(@"已达到最大重试次数，放弃获取");
                    return null;
                }
            }
        }

        return null;
    }

    private static bool TryLoadFromCache(out ModernManifest manifest, bool ignoreExpiry = false)
    {
        manifest = null;

        try
        {
            if (!File.Exists(CacheFilePath))
            {
                Console.WriteLine(@"缓存文件不存在");
                return false;
            }

            var jsonString = File.ReadAllText(CacheFilePath);
            var cache = JsonSerializer.Deserialize<ManifestCache>(jsonString);

            if (cache == null || cache.Manifest == null)
            {
                Console.WriteLine(@"缓存数据为空");
                return false;
            }

            if (!ignoreExpiry)
            {
                var cacheAge = DateTime.Now - cache.CacheTime;
                if (cacheAge > CacheMaxAge)
                {
                    Console.WriteLine($@"缓存已过期（{cacheAge.TotalHours:F1}小时前），将重新获取");
                    return false;
                }
            }
            else
            {
                var cacheAge = DateTime.Now - cache.CacheTime;
                Console.WriteLine($@"使用缓存数据（已过期 {cacheAge.TotalHours:F1} 小时，作为后备方案）");
            }

            manifest = cache.Manifest;
            Console.WriteLine(@"从缓存加载 ModernManifest 成功");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载缓存失败: {ex.Message}");
            try { ClearCache(); } catch { }
            return false;
        }
    }

    private static void SaveToCache(ModernManifest manifest)
    {
        try
        {
            var cacheDir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var cache = new ManifestCache
            {
                CacheTime = DateTime.Now,
                Manifest = manifest
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(cache, options);
            File.WriteAllText(CacheFilePath, jsonString);

            Console.WriteLine($@"ModernManifest 已缓存到文件: {CacheFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"保存缓存失败: {ex.Message}");
        }
    }
}