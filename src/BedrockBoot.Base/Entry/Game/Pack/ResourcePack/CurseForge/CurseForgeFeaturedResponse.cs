using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;

/// <summary>
/// 专门用于处理 CurseForge 推荐/热门资源 (Featured/Popular) 接口返回的数据格式
/// </summary>
public class CurseForgeFeaturedResponse
{
    [JsonPropertyName("data")] public FeaturedData Data { get; set; }

    public class FeaturedData
    {
        // 对应 JSON 中的 "featured" 数组
        [JsonPropertyName("featured")] 
        public List<CurseForgeResponse.ModData> Featured { get; set; } = new();

        // 对应 JSON 中的 "popular" 数组
        [JsonPropertyName("popular")] 
        public List<CurseForgeResponse.ModData> Popular { get; set; } = new();

        // 对应 JSON 中的 "recentlyUpdated" 数组 (如果接口包含)
        [JsonPropertyName("recentlyUpdated")] 
        public List<CurseForgeResponse.ModData> RecentlyUpdated { get; set; } = new();
    }
}