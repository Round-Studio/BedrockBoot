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
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Search;

namespace BedrockBoot.Models.Pack.Search
{
    public static class SearchFactory
    {
        private static readonly Dictionary<SearchResourceType, ISearch> _searchCache = new();

        public static ISearch GetSearch(SearchResourceType type)
        {
            if (_searchCache.TryGetValue(type, out var search))
                return search;

            search = type switch
            {
                SearchResourceType.Minecraft => new MinecraftSearch(),
                SearchResourceType.ResourcePack => new CurseForgeSearch(),
                SearchResourceType.PluginPack => new PluginPackSearch(),
                SearchResourceType.LeviLaminaMods => new LeviLaminaModSearch(),
                SearchResourceType.DllMods => new DllModsSearch(),
                _ => throw new NotSupportedException($"不支持的搜索类型: {type}")
            };

            _searchCache[type] = search;
            return search;
        }

        public static T GetSearch<T>() where T : ISearch
        {
            foreach (var search in _searchCache.Values)
            {
                if (search is T typedSearch)
                    return typedSearch;
            }

            throw new InvalidOperationException($"未找到类型 {typeof(T).Name} 的搜索实现");
        }
    }
}