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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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
            _totalPages = 1;
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
            if (_isSearching || _totalPages <= 0) return;

            var target = Math.Clamp(pageNumber, 1, _totalPages);
            if (target == _currentPage) return;

            _currentPage = target;
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
            SearchResultPage? result = null;
            Exception? error = null;

            try
            {
                result = await _currentSearch.SearchPageAsync(info.Key, _currentPage, PageSize);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isSearching = false;
                IsEdit = true;

                if (error != null)
                    UpdateUIWithError(error);
                else if (result != null)
                    UpdateUIWithResults(result);
            });
        }

        private void UpdateUIWithResults(SearchResultPage result)
        {
            LoadingRing.IsVisible = false;

            _totalPages = result.GetTotalPages(PageSize);

            if (_totalPages > 0 && _currentPage > _totalPages)
            {
                GoToPage(_totalPages);
                return;
            }

            if (result.Items.Count > 0)
            {
                ResultPage.Update(CreateResultsView(result.Items), _totalPages, _currentPage);
                ResultPage.IsVisible = true;
                NoneBox.IsVisible = false;
            }
            else
            {
                _totalPages = 0;
                ResultPage.IsVisible = false;
                NoneBox.IsVisible = true;
            }
        }

        private void UpdateUIWithError(Exception ex)
        {
            LoadingRing.IsVisible = false;
            _totalPages = 0;
            NoneBox.IsVisible = true;
            ResultPage.IsVisible = false;
            Console.WriteLine($@"搜索失败: {ex}");
        }

        private static ListBox CreateResultsView(List<SearchResultItemInfo> items)
        {
            // 用 ListBox + 数据模板承载结果，容器由 VirtualizingStackPanel 按需实例化，
            // 避免一页几十个条目（含图片缓存）同时常驻内存
            var list = new ListBox
            {
                Margin = new Thickness(20, 10, 20, 20),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemsSource = items,
                // 容器回收时 Content 会被清空，Avalonia 会以 null 调用模板，这里直接返回空控件
                ItemTemplate = new FuncDataTemplate<SearchResultItemInfo>((info, _) =>
                    info == null ? null : new SearchItem(info)),
                ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel())
            };

            // 去掉 OnePointUI 主题给 ListBoxItem 的内边距/最小高度/圆角，并让选中高亮立即取消
            list.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                    new Setter(TemplatedControl.MinHeightProperty, 0d),
                    new Setter(TemplatedControl.MinWidthProperty, 0d),
                    new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(0)),
                    new Setter(Layoutable.MarginProperty, new Thickness(0, 0, 0, 8))
                }
            });
            list.SelectionChanged += (_, _) =>
            {
                if (list.SelectedIndex != -1) list.SelectedIndex = -1;
            };

            return list;
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