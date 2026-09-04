using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Views.Pages.DownloadPage.SearchSubPage
{
    public partial class SearchDetailed : ISetting
    {
        private const int PageSize = 50;
        private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
        private ISearch _currentSearch;
        private bool _isSearching;
        private int _currentPage = 1;
        private int _totalPages;

        public SearchResourceType ChooseType => (SearchResourceType)ResourceTypeBox.SelectedIndex;

        public SearchDetailed()
        {
            InitializeComponent();
            DownloadSearch.SearchDetailed = this;
            RestoreLastSearchType();
            SetupPaginationActions();
        }

        public SearchDetailed(SearchInfo info) : this()
        {
            OnSearch(info);
        }

        public static SearchInfo SearchInfo { get; set; }

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

        private void GoToPage(int pageNumber)
        {
            if (_isSearching) return;
            _currentPage = Math.Clamp(pageNumber, 1, _totalPages);
            ExecuteSearch(SearchInfo);
        }

        private void ExecuteSearch(SearchInfo info)
        {
            if (_isSearching) return;

            PrepareSearchUI(info);
            SearchInfo = info;

            _currentSearch = SearchFactory.GetSearch(info.Type);
            SetupSearchExtraParameters();

            _isSearching = true;
            LoadingRing.IsVisible = true;
            NoneBox.IsVisible = false;

            Task.Run(() => PerformSearchAsync(info));
        }

        private void SetupSearchExtraParameters()
        {
            if (_currentSearch.SearchType == SearchResourceType.Minecraft)
            {
                _currentSearch.SetExtraParameter(GameType.SelectedIndex);
            }
            else if (_currentSearch.SearchType == SearchResourceType.ResourcePack)
            {
                _currentSearch.SetExtraParameter(CurseForgeResTypeBox.SelectedIndex);
            }
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
                CurseForgeResTypeBox.SelectedIndex = 0;
            }

            info.Key ??= "";
        }

        private async Task PerformSearchAsync(SearchInfo info)
        {
            try
            {
                var items = await _currentSearch.SearchAsync(info.Key, _currentPage, PageSize);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateUIWithResults(items));
            }
            catch (Exception ex)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => UpdateUIWithError(ex));
            }
            finally
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _isSearching = false;
                    IsEdit = true;
                });
            }
        }

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
                CurseForgeResTypeBox.SelectedIndex = 0;

            OnSearch(SearchInfo);
        }

        private void CurseForgeResTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!IsEdit) return;

            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;
            OnSearch(SearchInfo);
        }
    }
}