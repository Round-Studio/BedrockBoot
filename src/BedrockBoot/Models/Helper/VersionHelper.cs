using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockBoot.Models.Global;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Models.Helper;

public class VersionHelper
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static List<BuildInfo> _versions = null;
    private static readonly string CacheFilePath = Path.Combine(PathsList.TempPath, "version_cache.json");
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24); // 缓存24小时有效期
    private static readonly int MaxRetryCount = 3; // 最大重试次数
    
    static VersionHelper()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "mcappx_developer");
        
        var osVersion = Environment.OSVersion.VersionString;
        
        Console.WriteLine($@"OS Version: {osVersion}");
    
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            $"BedrockBoot/{Global.GlobalModel.BodyVersion} ({osVersion})");
    }
    
    // 缓存数据结构
    private class VersionCache
    {
        public DateTime CacheTime { get; set; }
        public List<BuildInfo> Versions { get; set; }
        public int VersionSourceIndex { get; set; }
        
        public VersionCache()
        {
            Versions = new List<BuildInfo>();
        }
    }
    
    public static List<BuildInfo> Versions => _versions;
    
    /// <summary>
    /// 强制刷新缓存，忽略缓存有效期
    /// </summary>
    public static List<BuildInfo> RefreshVersions()
    {
        _versions = null;
        return GetVersions(forceRefresh: true);
    }
    
    /// <summary>
    /// 清除缓存文件
    /// </summary>
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
    
    /// <summary>
    /// 获取缓存的年龄
    /// </summary>
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
    
    public static List<BuildInfo> GetVersions(bool forceRefresh = false)
    {
        // 如果内存中有缓存且不强制刷新，直接返回
        if (!forceRefresh && _versions != null) return _versions;
        
        // 尝试从文件缓存加载（未过期）
        if (!forceRefresh && TryLoadFromCache(out var cachedVersions, ignoreExpiry: false))
        {
            _versions = cachedVersions;
            return _versions;
        }
        
        // 从网络获取（带重试机制）
        var versions = FetchVersionsWithRetry();
        if (versions != null && versions.Count > 0)
        {
            _versions = versions;
            SaveToCache(versions);
            return _versions;
        }
        
        // 网络获取失败，优先读取本地缓存（忽略过期）
        if (TryLoadFromCache(out var fallbackVersions, ignoreExpiry: true))
        {
            Console.WriteLine(@"网络获取失败，使用本地缓存数据（可能已过期）");
            _versions = fallbackVersions;
            return _versions;
        }
        
        // 没有任何可用数据
        Console.WriteLine(@"无法获取版本列表：网络请求失败且无本地缓存");
        _versions = new List<BuildInfo>();
        return _versions;
    }
    
    /// <summary>
    /// 带重试机制的网络获取
    /// </summary>
    private static List<BuildInfo> FetchVersionsWithRetry()
    {
        // 首先尝试当前配置的源
        var currentUrl = GetVersionSourceUrl();
        var result = TryFetchFromUrl(currentUrl, 0);
        if (result != null) return result;
        
        // 如果当前源失败且不是源[0]，尝试源[0]
        var currentSourceIndex = GetCurrentSourceIndex();
        if (currentSourceIndex != 0)
        {
            Console.WriteLine($@"当前源 [{currentSourceIndex}] 获取失败，尝试使用源 [0] 重试");
            var defaultUrl = GetVersionSourceUrl(0);
            result = TryFetchFromUrl(defaultUrl, 0);
            if (result != null) return result;
        }
        
        // 尝试其他所有可用的源
        for (int i = 0; i < SourceList.VersionDataSources.Count; i++)
        {
            // 跳过已经尝试过的源
            if (i == currentSourceIndex || (currentSourceIndex != 0 && i == 0)) continue;
            
            Console.WriteLine($@"尝试使用备用源 [{i}]");
            var url = GetVersionSourceUrl(i);
            result = TryFetchFromUrl(url, i);
            if (result != null) return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// 尝试从指定URL获取版本信息（带重试）
    /// </summary>
    private static List<BuildInfo> TryFetchFromUrl(string url, int sourceIndex)
    {
        for (int retry = 0; retry < MaxRetryCount; retry++)
        {
            try
            {
                if (retry > 0)
                {
                    Console.WriteLine($@"第 {retry + 1} 次重试获取版本列表...");
                    // 重试前等待一段时间
                    System.Threading.Thread.Sleep(1000 * retry);
                }
                
                Console.WriteLine($@"从网络获取版本列表: {url}");
                var jsonString = _httpClient.GetStringAsync(url).Result;
                
                var versions = ParseVersionJson(jsonString);
                if (versions != null && versions.Count > 0)
                {
                    Console.WriteLine($@"成功获取 {versions.Count} 个版本");
                    return versions;
                }
                
                Console.WriteLine(@"获取到的版本列表为空");
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"从 {url} 获取版本失败 (尝试 {retry + 1}/{MaxRetryCount}): {ex.Message}");
                
                if (retry == MaxRetryCount - 1)
                {
                    Console.WriteLine($@"已达到最大重试次数，放弃该源");
                    return null;
                }
            }
        }
        
        return null;
    }
    
    private static List<BuildInfo> ParseVersionJson(string jsonString)
    {
        using var document = JsonDocument.Parse(jsonString);
        var root = document.RootElement;
        
        // 获取 From_mcappx.com 属性（或者动态获取第一个非 CreationTime 的属性）
        JsonElement versionsDict;
        
        if (root.TryGetProperty("From_mcappx.com", out var fromProperty))
        {
            versionsDict = fromProperty;
        }
        else
        {
            // 动态查找：跳过 CreationTime，取第一个属性
            var firstProp = root.EnumerateObject().FirstOrDefault(p => p.Name != "CreationTime");
            if (firstProp.Value.ValueKind == JsonValueKind.Undefined)
            {
                return new List<BuildInfo>();
            }
            versionsDict = firstProp.Value;
        }
        
        if (versionsDict.ValueKind != JsonValueKind.Object)
        {
            return new List<BuildInfo>();
        }
        
        var versionCache = new List<(BuildInfo Item, Version Version)>();
        
        foreach (var versionProperty in versionsDict.EnumerateObject())
        {
            var versionKey = versionProperty.Name;
            var buildInfoElement = versionProperty.Value;
            
            if (buildInfoElement.ValueKind != JsonValueKind.Object) continue;
            
            try
            {
                var buildInfo = JsonSerializer.Deserialize<BuildInfo>(buildInfoElement.GetRawText());
                if (buildInfo == null) continue;
                
                // 如果 ID 为空，使用字典的键作为 ID
                if (string.IsNullOrEmpty(buildInfo.ID))
                {
                    buildInfo.ID = versionKey;
                }
                
                // 验证必要字段
                if (string.IsNullOrEmpty(buildInfo.ID)) continue;
                if (buildInfo.Variations == null || buildInfo.Variations.Count == 0) continue;
                
                // 检查是否有有效的 Variation（至少有一个 MetaData 不为空）
                var hasValidVariation = buildInfo.Variations.Any(v => v.MetaData != null && v.MetaData.Count > 0);
                if (!hasValidVariation) continue;
                
                Version version = null;
                try
                {
                    version = new Version(buildInfo.ID);
                }
                catch
                {
                    // 版本号解析失败，忽略
                }
                
                versionCache.Add((buildInfo, version));
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"解析版本 {versionKey} 失败: {ex.Message}");
                continue;
            }
        }
        
        // 排序：有效的 Version 对象按降序排前面，无效的按字符串排序
        versionCache.Sort((x, y) =>
        {
            if (x.Version != null && y.Version != null)
                return y.Version.CompareTo(x.Version);
            if (x.Version != null) return -1;
            if (y.Version != null) return 1;
            return string.Compare(y.Item.ID, x.Item.ID, StringComparison.Ordinal);
        });
        
        return versionCache.Select(x => x.Item).ToList();
    }
    
    private static bool TryLoadFromCache(out List<BuildInfo> versions, bool ignoreExpiry = false)
    {
        versions = null;
        
        try
        {
            if (!File.Exists(CacheFilePath))
            {
                Console.WriteLine(@"缓存文件不存在");
                return false;
            }
            
            var jsonString = File.ReadAllText(CacheFilePath);
            var cache = JsonSerializer.Deserialize<VersionCache>(jsonString);
            
            if (cache == null || cache.Versions == null || cache.Versions.Count == 0)
            {
                Console.WriteLine(@"缓存数据为空");
                return false;
            }
            
            // 检查缓存是否过期（除非忽略过期检查）
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
            
            // 验证缓存使用的数据源是否与当前配置一致
            var currentSourceIndex = GetCurrentSourceIndex();
            if (cache.VersionSourceIndex != currentSourceIndex && !ignoreExpiry)
            {
                Console.WriteLine(@"缓存的数据源已更改，将重新获取");
                return false;
            }
            
            versions = cache.Versions;
            Console.WriteLine($@"从缓存加载版本列表成功，共 {versions.Count} 个版本");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"加载缓存失败: {ex.Message}");
            // 缓存文件损坏时清除它
            try { ClearCache(); } catch { }
            return false;
        }
    }
    
    private static void SaveToCache(List<BuildInfo> versions)
    {
        try
        {
            // 确保缓存目录存在
            var cacheDir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            
            var cache = new VersionCache
            {
                CacheTime = DateTime.Now,
                Versions = versions,
                VersionSourceIndex = GetCurrentSourceIndex()
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var jsonString = JsonSerializer.Serialize(cache, options);
            File.WriteAllText(CacheFilePath, jsonString);
            
            Console.WriteLine($@"版本列表已缓存到文件: {CacheFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"保存缓存失败: {ex.Message}");
        }
    }
    
    private static int GetCurrentSourceIndex()
    {
        if (GlobalModel.Config != null && 
            GlobalModel.Config.Data != null && 
            GlobalModel.Config.Data.VersionSourceIndex >= 0 &&
            GlobalModel.Config.Data.VersionSourceIndex < SourceList.VersionDataSources.Count)
        {
            return GlobalModel.Config.Data.VersionSourceIndex;
        }
        
        return 0;
    }
    
    private static string GetVersionSourceUrl(int? sourceIndex = null)
    {
        var index = sourceIndex ?? GetCurrentSourceIndex();
        
        // 确保索引有效
        if (index < 0 || index >= SourceList.VersionDataSources.Count)
        {
            index = 0;
        }
        
        var sources = SourceList.VersionDataSources.ToList();
        return sources[index].Value;
    }
}