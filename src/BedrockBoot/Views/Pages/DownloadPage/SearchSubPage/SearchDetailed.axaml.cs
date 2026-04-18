using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;

public partial class SearchDetailed : ISetting
{
    // 保存上一次的搜索类型
    private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
    private readonly CurseForgeApiClient _apiClient;
    private readonly int _pageSize = 50;
    private int _currentIndex;

    // 添加分页相关字段
    private int _currentPage = 1;

    // 添加搜索状态
    private bool _isSearching;
    private int _totalPages;

    public SearchDetailed()
    {
        InitializeComponent();
        _apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);

        DownloadSearch.SearchDetailed = this;

        // 恢复上一次的搜索类型
        if (ResourceTypeBox != null && _lastSearchType != SearchResourceType.Unknow)
            ResourceTypeBox.SelectedIndex = (int)_lastSearchType;

        // 设置上翻页逻辑
        ResultPage.UpAction = () =>
        {
            if (_currentPage > 1 && !_isSearching) GoToPage(_currentPage - 1);
        };

        // 设置下翻页逻辑
        ResultPage.DownAction = () =>
        {
            if (_currentPage < _totalPages && !_isSearching) GoToPage(_currentPage + 1);
        };
    }

    public SearchDetailed(SearchInfo info) : this()
    {
        OnSearch(info);
    }

    public static SearchInfo SearchInfo { get; set; }

    public void OnSearch(SearchInfo info)
    {
        // 保存搜索类型
        if (info.Type != SearchResourceType.Unknow)
        {
            _lastSearchType = info.Type;
            // 同时更新UI控件的选择
            if (ResourceTypeBox != null && ResourceTypeBox.SelectedIndex != (int)info.Type)
                ResourceTypeBox.SelectedIndex = (int)info.Type;
        }

        if (!string.IsNullOrEmpty(info.Key))
        {
            var searchHis = new ConfigEntity<List<SearchInfo>>(PathsList.HistoryPath);
            searchHis.Data.RemoveAll(x => x.Key == info.Key);
            searchHis.Data.Add(info);
            searchHis.Save();
        }
        
        // 重置分页状态
        _currentPage = 1;
        _currentIndex = 0;

        SearchWithPagination(info);
    }

    /// <summary>
    ///     跳转到指定页码
    /// </summary>
    private void GoToPage(int pageNumber)
    {
        if (_isSearching) return;

        _currentPage = Math.Clamp(pageNumber, 1, _totalPages);
        _currentIndex = (_currentPage - 1) * _pageSize;

        SearchWithPagination(SearchInfo);
    }

    /// <summary>
    ///     带分页的搜索
    /// </summary>
    private void SearchWithPagination(SearchInfo info)
    {
        if (_isSearching) return;

        IsEdit = false;

        MinecraftTypePanel.IsVisible = false;
        CurseForgeResTypePanel.IsVisible = false;
        ResultPage.IsVisible = false;

        if (info.Type != SearchResourceType.Unknow)
            ResourceTypeBox.SelectedIndex = (int)info.Type;
        else
            info.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

        if (info.Type == SearchResourceType.Minecraft)
            MinecraftTypePanel.IsVisible = true;
        if (info.Type == SearchResourceType.ResourcePack)
            CurseForgeResTypePanel.IsVisible = true;

        info.Key = string.IsNullOrEmpty(info.Key) ? "" : info.Key;

        SearchInfo = info;

        _isSearching = true;
        LoadingRing.IsVisible = true;
        NoneBox.IsVisible = false;

        // 开始搜索
        Task.Run(() =>
        {
            try
            {
                var items = new List<SearchResultItemInfo>();
                if (info.Type == SearchResourceType.Minecraft) // 游戏本体
                {
                    var allVersions = VersionHelper.GetVersions()
                        .Where(x => (x.ID.ToLower().Contains(info.Key) ||
                                     x.BuildType.ToString().ToLower().Contains(info.Key)) &&
                                    x.Type == (BedrockLauncher.Core.MinecraftGameTypeVersion)GameType.SelectedIndex)
#if LINUX
                        .Where(x=>x.BuildType == MinecraftBuildTypeVersion.GDK)           
#endif
                        .ToList();

                    // 计算总页数
                    _totalPages = (int)Math.Ceiling((double)allVersions.Count / _pageSize);

                    // 获取当前页的数据
                    var currentPageVersions = allVersions
                        .Skip(_currentIndex)
                        .Take(_pageSize)
                        .ToList();

                    currentPageVersions.ForEach(i =>
                    {
                        items.Add(new SearchResultItemInfo
                        {
                            Name = i.ID,
                            Description = $"{i.BuildType}, {i.Date}",
                            IconUri = i.Type == BedrockLauncher.Core.MinecraftGameTypeVersion.Release
                                ? "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png"
                                : "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png",
                            OnClick = s =>
                            {
                                GlobalModel.MainWindow.OpenDraw(new DrawDownloadGameContent(i), $"下载游戏 {i.ID}");
                            }
                        });
                    });
                }
                else if (info.Type == SearchResourceType.ResourcePack)
                {
                    var result = _apiClient.SearchModsAsync(SearchInfo.Key, pageSize: _pageSize, index: _currentIndex)
                        .Result;
                    _totalPages = (int)Math.Ceiling((double)result.Pagination.TotalCount / _pageSize);

                    var allResult = result.Data
                        .Where(x => x.Name.ToLower().Contains(SearchInfo.Key.ToLower()))
                        .ToList();

                    allResult.ForEach(i =>
                    {
                        var authorNames = i.Authors.Select(a => a.Name).ToList();

                        var categories = i.Categories.Select(a => a.Name).ToList();

                        var item = new SearchResultItemInfo
                        {
                            Name = i.Name,
                            Id = i.Id,
                            Description = $"{i.Summary}",
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
                        item.OnClick = s =>
                        {
                            // GlobalModel.MainWindow.OpenDraw(new DrawDownloadCurseForgeResourceContent(i),$"资源详细信息 {i.Name}");

                            DownloadRoot.Instance.NavigateTo(new ResultRoot(item));
                        };
                        items.Add(item);
                    });
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (items.Count > 0)
                    {
                        // 更新分页控件
                        ResultPage.Update(
                            CreateSearchResultsPage(items),
                            _totalPages,
                            _currentPage);

                        LoadingRing.IsVisible = false;
                        ResultPage.IsVisible = true;
                    }
                    else
                    {
                        LoadingRing.IsVisible = false;
                        NoneBox.IsVisible = true;
                        ResultPage.IsVisible = false;
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadingRing.IsVisible = false;
                    NoneBox.IsVisible = true;
                    ResultPage.IsVisible = false;
                    Console.WriteLine($@"搜索失败: {ex}");
                });
            }
            finally
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    _isSearching = false;
                    IsEdit = true;
                });
            }
        });
    }

    /// <summary>
    ///     创建搜索结果页面
    /// </summary>
    private ScrollViewer CreateSearchResultsPage(List<SearchResultItemInfo> items)
    {
        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20, 0, 20, 20),
            Spacing = 8
        };

        var resItems = items.Select(x => new SearchItem(x));

        stackPanel.Children.AddRange(resItems);

        var scrollViewer = new ScrollViewer
        {
            Content = stackPanel,
            Margin = new Thickness(0, 10, 0, 0)
        };

        return scrollViewer;
    }

    private void GameType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

            // 保存搜索类型
            _lastSearchType = SearchInfo.Type;

            OnSearch(SearchInfo);
        }
    }

    private void ResourceTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

            // 保存搜索类型
            _lastSearchType = SearchInfo.Type;

            OnSearch(SearchInfo);
        }
    }

    // 添加一个方法来获取和设置搜索类型
    public static SearchResourceType GetLastSearchType()
    {
        return _lastSearchType;
    }

    public static void SetLastSearchType(SearchResourceType searchType)
    {
        _lastSearchType = searchType;
    }
}