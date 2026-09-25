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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

namespace BedrockBoot.Models.Pack.Search
{
    public class PluginPackSearch : ISearch
    {
        private readonly MarketClient _marketClient;
        private bool _enableFuzzySearch;

        public async Task<List<SearchResultItemInfo>> GetRecommendAsync(int count = 2)
        {
            var result = (await SearchPageAsync("", 1, 100)).Items;

            if (result == null || result.Count == 0 || count <= 0)
                return new List<SearchResultItemInfo>();

            count = Math.Min(count, result.Count);

            var list = new List<SearchResultItemInfo>(result);
            var random = Random.Shared;

            for (int i = 0; i < count; i++)
            {
                int j = random.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list.Take(count).ToList();
        }

        public SearchResourceType SearchType => SearchResourceType.PluginPack;

        public PluginPackSearch()
        {
            _marketClient = new MarketClient();
            _enableFuzzySearch = Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;
        }

        public void SetExtraParameter(object parameter)
        {
        }

        public async Task<SearchResultPage> SearchPageAsync(string keyword, int page, int pageSize)
        {
            var result = await _marketClient.GetPluginsAsync();
            var filteredResult = result
                .Where(plugin => IsPluginMatch(plugin, keyword))
                .ToList();

            var currentIndex = (page - 1) * pageSize;
            var currentPagePlugins = filteredResult
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPagePlugins.ForEach(plugin =>
            {
                plugin.IconUrl = $"{SourceList.MarketApiHost}{plugin.IconUrl}";
                var item = new SearchResultItemInfo
                {
                    Name = plugin.PluginName,
                    Id = $"{plugin.RepositoryOwner}.{plugin.RepositoryName}",
                    Description = plugin.Description,
                    Authors = new List<string>() { plugin.Username },
                    ResourceType = SearchResourceType.PluginPack,
                    DownloadCount = 0,
                    IconUri = plugin.IconUrl,
                    Labels = plugin.Labels,
                    Images = null,
                    SourceWebsite = plugin.RepositoryUrl,
                    JsonData = JsonSerializer.Serialize(plugin)
                };
                item.OnClick = s =>
                {
                    Console.WriteLine($@"View Plugin: {s}");
                    DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                };
                items.Add(item);
            });

            return new SearchResultPage
            {
                Items = items,
                TotalCount = filteredResult.Count
            };
        }

        private bool IsPluginMatch(MarketResponse.PluginInfo plugin, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var pluginName = plugin.PluginName ?? string.Empty;
            var pluginDescription = plugin.Description ?? string.Empty;
            var repositoryUrl = plugin.RepositoryUrl ?? string.Empty;
            var username = plugin.Username ?? string.Empty;
            var combinedText = $"{pluginName} {pluginDescription} {repositoryUrl} {username}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(pluginName.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(pluginDescription.ToLower(), keywordLower, 0.6)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(repositoryUrl.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(username.ToLower(), keywordLower, 0.7)) return true;

            if (plugin.Labels != null)
            {
                foreach (var label in plugin.Labels)
                {
                    var labelName = label?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(labelName, keywordLower, 0.7)) return true;
                    if (labelName.Contains(keywordLower)) return true;
                }
            }

            return false;
        }
    }
}