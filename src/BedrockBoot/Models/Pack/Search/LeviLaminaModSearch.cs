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
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.Search
{
    public class LeviLaminaModSearch : ISearch
    {
        private bool _enableFuzzySearch;

        public SearchResourceType SearchType => SearchResourceType.LeviLaminaMods;
        public bool SupportsPagination => true;

        public LeviLaminaModSearch()
        {
            _enableFuzzySearch = Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;
        }

        public void SetExtraParameter(object parameter)
        {
        }

        public object GetExtraParameter() => null;

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
                    Id = 0,
                    Description = packageInfo.Description ?? string.Empty,
                    Authors = new List<string>(),
                    DownloadCount = 0,
                    IconUri = !string.IsNullOrEmpty(packageInfo.AvatarUrl)
                        ? packageInfo.AvatarUrl
                        : "avares://BedrockBoot/Assets/Icon/Files/NoneIcon.png",
                    Labels = packageInfo.Tags ?? new List<string>(),
                    Images = null,
                    SourceWebsite = $"https://github.com/{pkg.Key}",
                    JsonData = JsonSerializer.Serialize(pkg.Value)
                };

                item.OnClick = s =>
                {
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