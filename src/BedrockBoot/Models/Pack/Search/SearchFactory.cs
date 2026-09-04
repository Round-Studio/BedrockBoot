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