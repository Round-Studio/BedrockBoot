using System;
using System.Collections.Generic;
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

    public static List<BuildInfo> Versions => _versions;

    public static List<BuildInfo> GetVersions()
    {
        if (_versions != null) return _versions;

        try
        {
            var url = GetVersionSourceUrl();
            var jsonString = _httpClient.GetStringAsync(url).Result;
            
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
                    _versions = new List<BuildInfo>();
                    return _versions;
                }
                versionsDict = firstProp.Value;
            }
            
            if (versionsDict.ValueKind != JsonValueKind.Object)
            {
                _versions = new List<BuildInfo>();
                return _versions;
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
                    Console.WriteLine($"解析版本 {versionKey} 失败: {ex.Message}");
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
            
            _versions = versionCache.Select(x => x.Item).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取版本列表失败: {ex.Message}");
            _versions = new List<BuildInfo>();
        }
        
        return _versions;
    }
    
    private static string GetVersionSourceUrl()
    {
        // 从配置获取 URL，如果没有配置则使用默认
        if (GlobalModel.Config != null && 
            GlobalModel.Config.Data != null && 
            GlobalModel.Config.Data.VersionSourceIndex >= 0 &&
            GlobalModel.Config.Data.VersionSourceIndex < SourceList.VersionDataSources.Count)
        {
            return SourceList.VersionDataSources.ToList()[GlobalModel.Config.Data.VersionSourceIndex].Value;
        }

        GlobalModel.Config.Data.VersionSourceIndex = 0;
        GlobalModel.Config.Save();
        return GetVersionSourceUrl();
    }
}