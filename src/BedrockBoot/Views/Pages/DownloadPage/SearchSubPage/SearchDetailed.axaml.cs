using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Models.Pack.Plugin.Market;
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Enum;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDetailed : ISetting
{
    private const int PageSize = 50;
    private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
    private readonly CurseForgeApiClient _apiClient;
    private readonly MarketClient _marketClient;
    private bool _isSearching;
    private int? _selectedClassId;

    private static readonly int[] CurseForgeClassIds = [4984, 6913, 6929, 6940, 6925];
    private int _currentPage = 1;
    private int _totalPages;
    private int _currentIndex => (_currentPage - 1) * PageSize;
    private bool EnableFuzzySearch => Core.Global.GlobalModel.Config.Data.IsEnableFuzzySearch;

    public SearchResourceType ChooseType => (SearchResourceType)ResourceTypeBox.SelectedIndex;

    public SearchDetailed()
    {
        InitializeComponent();
        _apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
        _marketClient = new MarketClient();

        DownloadSearch.SearchDetailed = this;
        RestoreLastSearchType();
        SetupPaginationActions();
    }

    public SearchDetailed(SearchInfo info) : this()
    {
        OnSearch(info);
    }

    public static SearchInfo SearchInfo { get; set; }

    #region 初始化

    private void RestoreLastSearchType()
    {
        if (ResourceTypeBox != null && _lastSearchType != SearchResourceType.Unknow)
            ResourceTypeBox.SelectedIndex = (int)_lastSearchType;
    }

    private void SetupPaginationActions()
    {
        ResultPage.UpAction = () =>
        {
            if (_currentPage > 1 && !_isSearching) GoToPage(_currentPage - 1);
        };

        ResultPage.DownAction = () =>
        {
            if (_currentPage < _totalPages && !_isSearching) GoToPage(_currentPage + 1);
        };
    }

    #endregion

    #region 搜索入口

    public void OnSearch(SearchInfo info)
    {
        SaveSearchType(info.Type);
        SaveSearchHistory(info);

        _currentPage = 1;
        ExecuteSearch(info);
    }

    private void SaveSearchType(SearchResourceType type)
    {
        if (type != SearchResourceType.Unknow)
        {
            _lastSearchType = type;
            if (ResourceTypeBox != null && ResourceTypeBox.SelectedIndex != (int)type)
                ResourceTypeBox.SelectedIndex = (int)type;
        }
    }

    private static void SaveSearchHistory(SearchInfo info)
    {
        if (string.IsNullOrEmpty(info.Key)) return;

        var searchHis = new ConfigEntity<List<SearchInfo>>(PathsList.HistoryPath);
        searchHis.Data.RemoveAll(x => x.Key == info.Key);
        searchHis.Data.Add(info);
        searchHis.Save();
    }

    #endregion

    #region 分页控制

    private void GoToPage(int pageNumber)
    {
        if (_isSearching) return;

        _currentPage = Math.Clamp(pageNumber, 1, _totalPages);
        ExecuteSearch(SearchInfo);
    }

    #endregion

    #region 核心搜索逻辑

    private void ExecuteSearch(SearchInfo info)
    {
        if (_isSearching) return;

        PrepareSearchUI(info);
        SearchInfo = info;

        _isSearching = true;
        LoadingRing.IsVisible = true;
        NoneBox.IsVisible = false;

        Task.Run(() => PerformSearchAsync(info));
    }

    private void PrepareSearchUI(SearchInfo info)
    {
        IsEdit = false;
        ResultPage.IsVisible = false;

        MinecraftTypePanel.IsVisible = info.Type == SearchResourceType.Minecraft;
        CurseForgeResTypePanel.IsVisible = info.Type == SearchResourceType.ResourcePack;

        if (info.Type != SearchResourceType.Unknow)
            ResourceTypeBox.SelectedIndex = (int)info.Type;
        else
            info.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

        if (info.Type != SearchResourceType.ResourcePack)
        {
            _selectedClassId = null;
            CurseForgeResTypeBox.SelectedIndex = 0;
        }

        info.Key ??= "";
    }

    private async Task PerformSearchAsync(SearchInfo info)
    {
        try
        {
            var items = await SearchByTypeAsync(info);
            await Dispatcher.UIThread.InvokeAsync(() => UpdateUIWithResults(items));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => UpdateUIWithError(ex));
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isSearching = false;
                IsEdit = true;
            });
        }
    }

    private Task<List<SearchResultItemInfo>> SearchByTypeAsync(SearchInfo info)
    {
        return info.Type switch
        {
            SearchResourceType.Minecraft => SearchMinecraftAsync(info),
            SearchResourceType.ResourcePack => SearchResourcePacksAsync(info),
            SearchResourceType.PluginPack => SearchPluginPacksAsync(info),
            SearchResourceType.LeviLaminaMods => SearchLeviLaminaModsAsync(info),
            _ => Task.FromResult(new List<SearchResultItemInfo>())
        };
    }

    #endregion

    #region 模糊搜索工具方法

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; matrix[i, 0] = i++)
        {
        }

        for (var j = 0; j <= targetLength; matrix[0, j] = j++)
        {
        }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    private bool IsFuzzyMatch(string source, string keyword, double threshold = 0.7)
    {
        if (string.IsNullOrEmpty(keyword)) return true;
        if (string.IsNullOrEmpty(source)) return false;
        if (!EnableFuzzySearch) return source.ToLower().Contains(keyword.ToLower());

        var sourceLower = source.ToLower();
        var keywordLower = keyword.ToLower();

        if (sourceLower.Contains(keywordLower)) return true;

        var distance = CalculateLevenshteinDistance(sourceLower, keywordLower);
        var maxLength = Math.Max(sourceLower.Length, keywordLower.Length);
        var similarity = 1.0 - (double)distance / maxLength;

        return similarity >= threshold;
    }

    private static string ExtractNumbers(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return new string(input.Where(char.IsDigit).ToArray());
    }

    private static string RemoveAllZeros(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("0", string.Empty);
    }

    private static string ProcessMinecraftVersionForFuzzy(string version)
    {
        if (string.IsNullOrEmpty(version)) return string.Empty;
        var numbers = ExtractNumbers(version);
        return RemoveAllZeros(numbers);
    }

    #endregion

    #region Minecraft 搜索

    private Task<List<SearchResultItemInfo>> SearchMinecraftAsync(SearchInfo info)
    {
        return Task.Run(() =>
        {
            var allVersions = VersionHelper.GetVersions()
                .Where(x => x.Type == (MinecraftGameTypeVersion)GameType.SelectedIndex)
#if LINUX
                .Where(x => x.BuildType == MinecraftBuildTypeVersion.GDK)
#endif
                .ToList();

            var filteredVersions = allVersions
                .Where(version => IsMinecraftMatch(version, info.Key))
                .ToList();

            var inputParts = info.Key.Split('.');
            if (inputParts.Length >= 2 && inputParts[0] == "1" && int.TryParse(inputParts[1], out int second) &&
                second >= 26)
            {
                inputParts = inputParts.Skip(1).ToArray();
            }

            int MatchCount(string version)
            {
                var parts = version.Split('.');
                int count = 0;
                int len = Math.Min(parts.Length, inputParts.Length);

                for (int i = 0; i < len; i++)
                {
                    if (parts[i] == inputParts[i])
                        count++;
                    else
                        break;
                }

                return count;
            }

            filteredVersions = filteredVersions
                .OrderByDescending(x => MatchCount(x.Key))
                .ToList();

            _totalPages = (int)Math.Ceiling((double)filteredVersions.Count / PageSize);

            var currentPageVersions = filteredVersions
                .Skip(_currentIndex)
                .Take(PageSize)
                .ToList();

            var items = new List<SearchResultItemInfo>();
            currentPageVersions.ForEach(i =>
            {
                items.Add(new SearchResultItemInfo
                {
                    Name = i.Key,
                    Description = $"{i.ID}, {i.BuildType}, {i.Date}",
                    IconUri = i.Type == MinecraftGameTypeVersion.Release
                        ? "avares://BedrockBoot/Assets/Icon/Logo/Grass.png"
                        : "avares://BedrockBoot/Assets/Icon/Logo/GrassScript.png",
                    OnClick = s =>
                    {
                        GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(i),
                            $"{I18nManager.Instance["Download.Action.DownloadGame"]} {i.Key}");
                    }
                });
            });

            return items;
        });
    }

    private bool IsMinecraftMatch(dynamic version, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return true;

        keyword = keyword.Trim();

        var versionId = version.ID ?? string.Empty;
        var versionKey = version.Key ?? string.Empty;
        var buildType = version.BuildType?.ToString() ?? string.Empty;

        if (keyword.ToLower() == "uwp") return buildType == "UWP";
        else if (keyword.ToLower() == "gdk") return buildType == "GDK";

        if (keyword.StartsWith("1."))
        {
            if (versionId.StartsWith(keyword) || versionKey.StartsWith(keyword)) return true;
        }
        else if (versionId.Contains(keyword) || versionKey.Contains(keyword)) return true;

        var parts = keyword.Split('.');
        if (parts.Length == 4)
        {
            if (parts[0] == "1")
            {
                return ("." + versionKey + ".").Contains($".{parts[1]}.{parts[2]}.") && !versionKey.StartsWith("0.");
            }
        }
        else if (parts.Length == 3)
        {
            if (parts[0] != "1")
            {
                return ("." + versionKey + ".").Contains($".{parts[0]}.{parts[1]}.");
            }
        }

        return false;
    }

    #endregion

    #region 资源包搜索

    private async Task<List<SearchResultItemInfo>> SearchResourcePacksAsync(SearchInfo info)
    {
        var result =
            await _apiClient.SearchModsAsync(info.Key, PageSize, classId: _selectedClassId, index: _currentIndex);
        _totalPages = (int)Math.Ceiling((double)result.Pagination.TotalCount / PageSize);

        var filteredData = result.Data
            .Where(mod => IsResourcePackMatch(mod, info.Key))
            .ToList();

        var items = new List<SearchResultItemInfo>();
        filteredData.ForEach(i =>
        {
            var authorNames = i.Authors.Select(a => a.Name).ToList();
            var categories = i.Categories.Select(a => a.Name).ToList();

            var item = new SearchResultItemInfo
            {
                Name = i.Name,
                Id = i.Id,
                Description = i.Summary,
                DateUpdated = i.DateReleased,
                DateCreated = i.DateCreated,
                Authors = authorNames,
                DownloadCount = (uint)i.DownloadCount,
                IconUri = i.Logo.Url,
                Labels = categories,
                Images = i.Screenshots.Select(a => a.Url).ToList(),
                SourceWebsite = i.Links.WebsiteUrl,
                JsonData = JsonSerializer.Serialize(i)
            };
            item.OnClick = s => { DownloadRoot.Instance.NavigateTo(new ResultRoot(item)); };
            items.Add(item);
        });

        return items;
    }

    private bool IsResourcePackMatch(dynamic mod, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return true;

        var modName = mod.Name ?? string.Empty;
        var modSummary = mod.Summary ?? string.Empty;
        var combinedText = $"{modName} {modSummary}".ToLower();

        if (!EnableFuzzySearch)
        {
            return combinedText.Contains(keyword.ToLower());
        }

        var keywordLower = keyword.ToLower();

        if (combinedText.Contains(keywordLower)) return true;

        if (IsFuzzyMatch(modName.ToLower(), keywordLower, 0.7)) return true;

        if (IsFuzzyMatch(modSummary.ToLower(), keywordLower, 0.6)) return true;

        if (mod.Authors != null)
        {
            foreach (var author in mod.Authors)
            {
                var authorName = author.Name?.ToLower() ?? string.Empty;
                if (IsFuzzyMatch(authorName, keywordLower, 0.7)) return true;
                if (authorName.Contains(keywordLower)) return true;
            }
        }

        if (mod.Categories != null)
        {
            foreach (var category in mod.Categories)
            {
                var categoryName = category.Name?.ToLower() ?? string.Empty;
                if (IsFuzzyMatch(categoryName, keywordLower, 0.7)) return true;
                if (categoryName.Contains(keywordLower)) return true;
            }
        }

        return false;
    }

    #endregion

    #region 插件搜索

    private async Task<List<SearchResultItemInfo>> SearchPluginPacksAsync(SearchInfo info)
    {
        var result = await _marketClient.GetPluginsAsync();
        var filteredResult = result
            .Where(plugin => IsPluginMatch(plugin, info.Key))
            .ToList();

        _totalPages = (int)Math.Ceiling((double)filteredResult.Count / PageSize);

        var items = new List<SearchResultItemInfo>();
        filteredResult.ForEach(plugin =>
        {
            plugin.IconUrl = $"{SourceList.MarketApiHost}{plugin.IconUrl}";
            var item = new SearchResultItemInfo
            {
                Name = plugin.PluginName,
                Id = 0,
                Description = plugin.Description,
                Authors = new List<string>() { plugin.Username },
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
                GlobalModel.MainWindow.OpenDraw(new DrawDownloadPluginContent(plugin),
                    $"插件详细信息：{plugin.PluginName}");
            };
            items.Add(item);
        });

        return items;
    }

    private bool IsPluginMatch(MarketResponse.PluginInfo plugin, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return true;

        var pluginName = plugin.PluginName ?? string.Empty;
        var pluginDescription = plugin.Description ?? string.Empty;
        var repositoryUrl = plugin.RepositoryUrl ?? string.Empty;
        var username = plugin.Username ?? string.Empty;
        var combinedText = $"{pluginName} {pluginDescription} {repositoryUrl} {username}".ToLower();

        if (!EnableFuzzySearch)
        {
            return combinedText.Contains(keyword.ToLower());
        }

        var keywordLower = keyword.ToLower();

        if (combinedText.Contains(keywordLower)) return true;

        if (IsFuzzyMatch(pluginName.ToLower(), keywordLower, 0.7)) return true;

        if (IsFuzzyMatch(pluginDescription.ToLower(), keywordLower, 0.6)) return true;

        if (IsFuzzyMatch(repositoryUrl.ToLower(), keywordLower, 0.7)) return true;

        if (IsFuzzyMatch(username.ToLower(), keywordLower, 0.7)) return true;

        if (plugin.Labels != null)
        {
            foreach (var label in plugin.Labels)
            {
                var labelName = label?.ToLower() ?? string.Empty;
                if (IsFuzzyMatch(labelName, keywordLower, 0.7)) return true;
                if (labelName.Contains(keywordLower)) return true;
            }
        }

        return false;
    }

    #endregion

    #region LeviLamina 模组搜索

    private async Task<List<SearchResultItemInfo>> SearchLeviLaminaModsAsync(SearchInfo info)
    {
        var liprData = await LiprSource.GetDataAsync();

        var filteredPackages = liprData.Packages
            .Where(pkg => IsLeviLaminaModMatch(pkg.Key, pkg.Value, info.Key))
            .ToList();

        _totalPages = (int)Math.Ceiling((double)filteredPackages.Count / PageSize);

        var currentPagePackages = filteredPackages
            .Skip(_currentIndex)
            .Take(PageSize)
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
                    new DialogChooseLeviLaminaModVersionContent(pkg.Value.Variants["client"].Versions.Keys.ToList());
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

        if (!EnableFuzzySearch)
        {
            return combinedText.Contains(keyword.ToLower());
        }

        var keywordLower = keyword.ToLower();

        if (combinedText.Contains(keywordLower)) return true;

        if (IsFuzzyMatch(packageName.ToLower(), keywordLower, 0.7)) return true;

        if (IsFuzzyMatch(packageDescription.ToLower(), keywordLower, 0.6)) return true;

        if (IsFuzzyMatch(packageKey.ToLower(), keywordLower, 0.7)) return true;

        if (package.Info?.Tags != null)
        {
            foreach (var tag in package.Info.Tags)
            {
                var tagName = tag?.ToLower() ?? string.Empty;
                if (IsFuzzyMatch(tagName, keywordLower, 0.7)) return true;
                if (tagName.Contains(keywordLower)) return true;
            }
        }

        return false;
    }

    #endregion

    #region UI更新

    private void UpdateUIWithResults(List<SearchResultItemInfo> items)
    {
        LoadingRing.IsVisible = false;

        if (items.Count > 0)
        {
            ResultPage.Update(CreateResultsScrollViewer(items), _totalPages, _currentPage);
            ResultPage.IsVisible = true;
            NoneBox.IsVisible = false;
        }
        else
        {
            ResultPage.IsVisible = false;
            NoneBox.IsVisible = true;
        }
    }

    private void UpdateUIWithError(Exception ex)
    {
        LoadingRing.IsVisible = false;
        NoneBox.IsVisible = true;
        ResultPage.IsVisible = false;
        Console.WriteLine($@"搜索失败: {ex}");
    }

    private static ScrollViewer CreateResultsScrollViewer(List<SearchResultItemInfo> items)
    {
        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20, 0, 20, 20),
            Spacing = 8
        };

        var resItems = items.Select(x => new SearchItem(x));
        stackPanel.Children.AddRange(resItems);

        return new ScrollViewer
        {
            Content = stackPanel,
            Margin = new Thickness(0, 10, 0, 0)
        };
    }

    #endregion

    #region 事件处理

    private void HelpBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new DialogInfo
        {
            Title = "找不到想要的版本？",
            Content =
                "1. 请确保正式版、预览版、Beta 版选择正确。注意预览版和 Beta 版是两个不同的版本类型\n2. Windows 和 Android 的内部版本号格式不一致。\n   例如在 Android 上的 1.26.30.5 对应 Windows 上的 1.26.3005\n   请以游戏主屏幕右下角的版本号为准，例如上述版本的版本号为 26.30",
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
        });
    }

    private void GameType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;

        SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
        SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
        _lastSearchType = SearchInfo.Type;

        OnSearch(SearchInfo);
    }

    private void ResourceTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;

        SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
        SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
        _lastSearchType = SearchInfo.Type;

        MinecraftTypePanel.IsVisible = SearchInfo.Type == SearchResourceType.Minecraft;
        CurseForgeResTypePanel.IsVisible = SearchInfo.Type == SearchResourceType.ResourcePack;

        if (SearchInfo.Type != SearchResourceType.ResourcePack)
            _selectedClassId = null;

        OnSearch(SearchInfo);
    }

    private void CurseForgeResTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEdit) return;

        var index = CurseForgeResTypeBox.SelectedIndex;
        _selectedClassId = index > 0 && index <= CurseForgeClassIds.Length
            ? CurseForgeClassIds[index - 1]
            : null;

        SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
        SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
        OnSearch(SearchInfo);
    }

    #endregion
}