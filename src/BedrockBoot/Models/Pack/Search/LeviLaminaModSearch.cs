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
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Enum.Search;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

namespace BedrockBoot.Models.Pack.Search
{
    public class LeviLaminaModSearch : ISearch
    {
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

        public SearchResourceType SearchType => SearchResourceType.LeviLaminaMods;

        public LeviLaminaModSearch()
        {
            _enableFuzzySearch = Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;
        }

        public void SetExtraParameter(object parameter)
        {
        }

        public async Task<SearchResultPage> SearchPageAsync(string keyword, int page, int pageSize)
        {
            var index = await LeviModsSource.GetDataAsync();

            // 启动器面向客户端实例安装，只保留带 client 端侧的模组
            var clientMods = (index?.Mods ?? new List<LeviModEntry>())
                .Where(mod => mod.Side?.Contains("client") == true)
                .ToList();

            var filteredMods = clientMods
                .Where(mod => IsLeviLaminaModMatch(mod, keyword))
                .ToList();

            // 命中名称的排前面，其余保持接口返回的相关性顺序
            if (!string.IsNullOrEmpty(keyword))
            {
                var keywordLower = keyword.ToLower();
                filteredMods = filteredMods
                    .OrderBy(mod => (mod.Name ?? "").ToLower().Contains(keywordLower) ? 0 : 1)
                    .ThenBy(mod => mod.Id, StringComparer.Ordinal)
                    .ToList();
            }

            var currentIndex = (page - 1) * pageSize;
            var currentPageMods = filteredMods
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            foreach (var mod in currentPageMods)
            {
                var item = new SearchResultItemInfo
                {
                    Name = string.IsNullOrEmpty(mod.Name) ? mod.Id : mod.Name,
                    Id = mod.Id,
                    Description = mod.Description ?? string.Empty,
                    Authors = new List<string> { mod.Owner ?? mod.Id },
                    DownloadCount = (uint)Math.Max(0, mod.Downloads),
                    IconUri = !string.IsNullOrWhiteSpace(mod.Logo)
                        ? mod.Logo
                        : "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png",
                    Labels = mod.Tags ?? new List<string>(),
                    Images = null,
                    SourceWebsite = mod.Url,
                    JsonData = JsonSerializer.Serialize(mod),
                    DateUpdated = mod.Updated,
                    ResourceType = SearchResourceType.LeviLaminaMods
                };

                item.OnClick = s =>
                {
                    DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                };
                items.Add(item);
            }

            return new SearchResultPage
            {
                Items = items,
                TotalCount = filteredMods.Count
            };
        }

        private bool IsLeviLaminaModMatch(LeviModEntry mod, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var modName = string.IsNullOrEmpty(mod.Name) ? mod.Id : mod.Name;
            var modDescription = mod.Description ?? string.Empty;
            var modTags = mod.Tags != null ? string.Join(" ", mod.Tags) : string.Empty;
            var combinedText = $"{modName} {modDescription} {modTags} {mod.Id} {mod.Owner}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(modName.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(modDescription.ToLower(), keywordLower, 0.6)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(mod.Id.ToLower(), keywordLower, 0.7)) return true;

            if (mod.Tags != null)
            {
                foreach (var tag in mod.Tags)
                {
                    var tagName = tag?.ToLower() ?? string.Empty;
                    if (tagName.Contains(keywordLower)) return true;
                    if (FuzzyMatchHelper.IsFuzzyMatch(tagName, keywordLower, 0.7)) return true;
                }
            }

            return false;
        }
    }
}
