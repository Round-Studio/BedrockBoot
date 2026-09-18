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