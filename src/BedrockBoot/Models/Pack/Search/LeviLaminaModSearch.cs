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
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.Search
{
    public class LeviLaminaModSearch : ISearch
    {
        private bool _enableFuzzySearch;

        public async Task<List<SearchResultItemInfo>> GetRecommendAsync(int count = 2)
        {
            var result = await SearchAsync("", 1, 100);

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

        public Task<List<SearchResultItemInfo>> SearchAsync(string keyword)
        {
            return SearchAsync(keyword, 1, 50);
        }

        public async Task<List<SearchResultItemInfo>> SearchAsync(string keyword, int page, int pageSize)
        {
            var liprData = await LiprSource.GetDataAsync();

            var filteredPackages = liprData.Packages
                .Where(pkg => IsLeviLaminaModMatch(pkg.Key, pkg.Value, keyword))
                .ToList();

            var currentIndex = (page - 1) * pageSize;
            var currentPagePackages = filteredPackages
                .Skip(currentIndex)
                .Take(pageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPagePackages.ForEach(pkg =>
            {
                var packageInfo = pkg.Value.Info;
                var item = new SearchResultItemInfo
                {
                    Name = packageInfo.Name ?? pkg.Key,
                    Id = $"{pkg.Key.Split('/')[1]}.{pkg.Key.Split('/')[2]}",
                    Description = packageInfo.Description ?? string.Empty,
                    Authors = new List<string>() { pkg.Key.Split('/')[1] },
                    DownloadCount = 0,
                    IconUri = !string.IsNullOrEmpty(packageInfo.AvatarUrl)
                        ? packageInfo.AvatarUrl
                        : "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png",
                    Labels = packageInfo.Tags ?? new List<string>(),
                    Images = null,
                    SourceWebsite = $"https://{pkg.Key}",
                    JsonData = JsonSerializer.Serialize(pkg),
                    DateUpdated = pkg.Value.UpdatedAt,
                    ResourceType = SearchResourceType.LeviLaminaMods
                };

                item.OnClick = s =>
                {
                    DownloadRoot.Instance.NavigateTo(new ResultRoot(item));

                    return;
                    var chooseVersionDialog =
                        new DialogChooseLeviLaminaModVersionContent(pkg.Value.Variants["client"].Versions.Keys
                            .ToList());
                    DialogHost.Show(new()
                    {
                        Title = $"选择 {packageInfo.Name} 版本",
                        Content = chooseVersionDialog,
                        CloseButtonText = "确定",
                        PrimaryButtonText = "取消",
                        AccountButton = DialogButtons.CloseButton,
                        CloseAction = () =>
                        {
                            var chooseModVersion = chooseVersionDialog.ChooseVersion;
                            var chooseInstanceDialog = new DialogChooseLeviLaminaModInstallInstanceContent(pkg.Value,
                                chooseModVersion, pkg.Value.Variants["client"].Versions[chooseModVersion]);
                            DialogHost.Show(new()
                            {
                                Title = $"安装 {packageInfo.Name} {chooseVersionDialog.ChooseVersion}",
                                Content = chooseInstanceDialog,
                                CloseButtonText = "确定",
                                PrimaryButtonText = "取消",
                                AccountButton = DialogButtons.CloseButton,
                                CloseAction = () =>
                                {
                                    var installer = new LeviLaminaModsInstaller(pkg.Value, pkg.Key);
                                    installer.Install(chooseModVersion, chooseInstanceDialog.SavePath);
                                }
                            });
                        }
                    });
                };
                items.Add(item);
            });

            return items;
        }

        private bool IsLeviLaminaModMatch(string packageKey, PackageInfo package, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return true;

            var packageName = package.Info?.Name ?? packageKey;
            var packageDescription = package.Info?.Description ?? string.Empty;
            var packageTags = package.Info?.Tags != null ? string.Join(" ", package.Info.Tags) : string.Empty;
            var combinedText = $"{packageName} {packageDescription} {packageTags} {packageKey}".ToLower();

            if (!_enableFuzzySearch)
            {
                return combinedText.Contains(keyword.ToLower());
            }

            var keywordLower = keyword.ToLower();

            if (combinedText.Contains(keywordLower)) return true;

            if (FuzzyMatchHelper.IsFuzzyMatch(packageName.ToLower(), keywordLower, 0.7)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(packageDescription.ToLower(), keywordLower, 0.6)) return true;
            if (FuzzyMatchHelper.IsFuzzyMatch(packageKey.ToLower(), keywordLower, 0.7)) return true;

            if (package.Info?.Tags != null)
            {
                foreach (var tag in package.Info.Tags)
                {
                    var tagName = tag?.ToLower() ?? string.Empty;
                    if (FuzzyMatchHelper.IsFuzzyMatch(tagName, keywordLower, 0.7)) return true;
                    if (tagName.Contains(keywordLower)) return true;
                }
            }

            return false;
        }
    }
}