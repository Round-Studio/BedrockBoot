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
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

namespace BedrockBoot.Models.Pack.Search
{
    public class CurseForgeSearch : ISearch
    {
        private readonly CurseForgeApiClient _apiClient;
        private int? _selectedClassId;
        private bool _enableFuzzySearch;
        private static readonly int[] CurseForgeClassIds = [4984, 6913, 6929, 6940, 6925];

        public async Task<List<SearchResultItemInfo>> GetRecommendAsync(int count = 3)
        {
            var result = await _apiClient.GetFeaturedModsAsync();
            var items = new List<SearchResultItemInfo>();
            result.Data.Featured.ForEach(i =>
            {
                var authorNames = i.Authors.Select(a => a.Name).ToList();
                var categories = i.Categories.Select(a => a.Name).ToList();
                var item = new SearchResultItemInfo
                {
                    Name = i.Name,
                    Id = i.Id.ToString(),
                    Description = i.Summary,
                    DateUpdated = i.DateReleased,
                    DateCreated = i.DateCreated,
                    Authors = authorNames,
                    DownloadCount = (uint)i.DownloadCount,
                    IconUri = i.Logo.Url,
                    Labels = categories,
                    Images = i.Screenshots.Select(a => a.Url).ToList(),
                    SourceWebsite = i.Links.WebsiteUrl,
                    JsonData = JsonSerializer.Serialize(i),
                    ResourceType = SearchResourceType.ResourcePack
                };
                item.OnClick = s => { DownloadRoot.Instance.NavigateTo(new ResultRoot(item)); };
                items.Add(item);
            });

            return items;
        }

        public SearchResourceType SearchType => SearchResourceType.ResourcePack;

        public CurseForgeSearch()
        {
            _apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
            _enableFuzzySearch = Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;
        }

        public void SetExtraParameter(object parameter)
        {
            if (parameter is int classIdIndex)
            {
                _selectedClassId = classIdIndex > 0 && classIdIndex <= CurseForgeClassIds.Length
                    ? CurseForgeClassIds[classIdIndex - 1]
                    : null;
            }
        }

        public async Task<SearchResultPage> SearchPageAsync(string keyword, int page, int pageSize)
        {
            var currentIndex = (page - 1) * pageSize;
            var result =
                await _apiClient.SearchModsAsync(keyword, pageSize, classId: _selectedClassId, index: currentIndex);

            var filteredData = result.Data
                .Where(mod => IsResourcePackMatch(mod, keyword))
                .ToList();

            var items = new List<SearchResultItemInfo>();
            filteredData.ForEach(i =>
            {
                var authorNames = i.Authors.Select(a => a.Name).ToList();
                var categories = i.Categories.Select(a => a.Name).ToList();

                var item = new SearchResultItemInfo
                {
                    Name = i.Name,
                    Id = i.Id.ToString(),
                    Description = i.Summary,
                    DateUpdated = i.DateReleased,
                    DateCreated = i.DateCreated,
                    Authors = authorNames,
                    DownloadCount = (uint)i.DownloadCount,
                    IconUri = i.Logo.Url,
                    Labels = categories,
                    Images = i.Screenshots.Select(a => a.Url).ToList(),
                    SourceWebsite = i.Links.WebsiteUrl,
                    JsonData = JsonSerializer.Serialize(i),
                    ResourceType = SearchResourceType.ResourcePack
                };
                item.OnClick = s => { DownloadRoot.Instance.NavigateTo(new ResultRoot(item)); };
                items.Add(item);
            });

            return new SearchResultPage
            {
                Items = items,
                TotalCount = result.Pagination?.TotalCount ?? items.Count
            };
        }

        private bool IsResourcePackMatch(dynamic mod, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var modName = mod.Name ?? string.Empty;
            var modSummary = mod.Summary ?? string.Empty;
            var combinedText = $"{modName} {modSummary}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(modName.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(modSummary.ToLower(), keywordLower, 0.6)) return true;

            if (mod.Authors != null)
            {
                foreach (var author in mod.Authors)
                {
                    var authorName = author.Name?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(authorName, keywordLower, 0.7)) return true;
                    if (authorName.Contains(keywordLower)) return true;
                }
            }

            if (mod.Categories != null)
            {
                foreach (var category in mod.Categories)
                {
                    var categoryName = category.Name?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(categoryName, keywordLower, 0.7)) return true;
                    if (categoryName.Contains(keywordLower)) return true;
                }
            }

            return false;
        }
    }
}