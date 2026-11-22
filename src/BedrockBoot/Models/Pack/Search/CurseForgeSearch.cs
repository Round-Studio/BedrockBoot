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
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Standard.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

namespace BedrockBoot.Models.Pack.Search
{
    public class CurseForgeSearch : ISearch
    {
        private readonly CurseForgeApiClient _apiClient;
        private CurseForgeSearchFilter _filter = new(null, null);
        private bool _enableFuzzySearch;

        public sealed record CurseForgeSearchFilter(int? ClassId, int? CategoryId);

        private static readonly Dictionary<int, string> ClassTranslationContexts = new()
        {
            [4984] = "Addons",
            [6913] = "Maps",
            [6929] = "TexturePacks",
            [6940] = "Scripts",
            [6925] = "Skins"
        };

        public static string TranslateCategory(CurseForgeResponse.Category category, int? classId = null)
        {
            var categoryClassId = category.ClassId > 0 ? category.ClassId : classId;
            if (categoryClassId == null || !ClassTranslationContexts.TryGetValue(categoryClassId.Value, out var context))
                return category.Name;

            //if (category.Name.Contains("3D Packs", StringComparison.OrdinalIgnoreCase))
            //    Console.WriteLine($"[CurseForge] TAG 原始名称: '{category.Name}' (Length={category.Name.Length})");

            var categoryName = category.Name.Trim();
            var translationName = categoryName.Equals("l 3D Packs", StringComparison.OrdinalIgnoreCase)
                ? "3D Packs"
                : categoryName;
            var key = $"CurseForge.Tag.{context}.{translationName}";
            if (Avalonia.Application.Current!.Resources.TryGetResource(key, null, out var value) && value is string text)
                return text;

            Console.WriteLine($"[CurseForge] 未找到 TAG 翻译: classId={categoryClassId}, name='{category.Name}', key='{key}'");
            return category.Name;
        }

        public async Task<List<SearchResultItemInfo>> GetRecommendAsync(int count = 3)
        {
            var result = await _apiClient.GetFeaturedModsAsync();
            var items = new List<SearchResultItemInfo>();
            result.Data.Featured.ForEach(i =>
            {
                var authorNames = i.Authors.Select(a => a.Name).ToList();
                var categories = i.Categories.Select(category => TranslateCategory(category, i.ClassId)).ToList();
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
            if (parameter is CurseForgeSearchFilter filter)
                _filter = filter;
        }

        public async Task<SearchResultPage> SearchPageAsync(string keyword, int page, int pageSize)
        {
            var currentIndex = (page - 1) * pageSize;
            var result = await _apiClient.SearchModsAsync(keyword, pageSize,
                classId: _filter.ClassId, index: currentIndex, categoryId: _filter.CategoryId);

            var filteredData = result.Data
                .Where(mod => IsResourcePackMatch(mod, keyword))
                .ToList();

            var items = new List<SearchResultItemInfo>();
            filteredData.ForEach(i =>
            {
                var authorNames = i.Authors.Select(a => a.Name).ToList();
                var categories = i.Categories.Select(category => TranslateCategory(category, i.ClassId)).ToList();

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