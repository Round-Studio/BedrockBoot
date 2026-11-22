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
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Standard.Enum.Search;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Search;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Standard.Interface;
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
        private SearchInfo? _pendingSearch;
        private bool _pendingSearchSaveHistory = true;
        private SearchResultPage? _displayedResult;
        private int _displayedPage;
        private int _displayedTotalPages;
        private int _currentPage = 1;
        private int _totalPages;
        private List<CurseForgeResponse.Category> _curseForgeCategories = new();
        private bool _isUpdatingCurseForgeFilters = true;
        private static readonly Dictionary<int, string> CurseForgeClassNames = new()
        {
            [4984] = "Download.CurseForge.ResourceType.Addons",
            [6913] = "Download.CurseForge.ResourceType.Maps",
            [6929] = "Download.CurseForge.ResourceType.TexturePacks",
            [6940] = "Download.CurseForge.ResourceType.Scripts",
            [6925] = "Download.CurseForge.ResourceType.Skins"
        };

        public SearchResourceType ChooseType => (SearchResourceType)ResourceTypeBox.SelectedIndex;

        public SearchDetailed()
        {
            InitializeComponent();
            UpdateCurseForgeCategoryOptions();
            _isUpdatingCurseForgeFilters = false;
            DownloadSearch.SearchDetailed = this;
            RestoreLastSearchType();
            SetupPaginationActions();
            _ = LoadCurseForgeCategoriesAsync();
            _ = ApplyRegionGateAsync();
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

        /// <summary>
        ///     境外设备（不含港澳台）隐藏"插件"与"Dll 模组"搜索类型选项；
        ///     通过下拉项 IsVisible 隐藏而非移除，保持 SelectedIndex 与枚举值的对应关系。
        /// </summary>
        private async Task ApplyRegionGateAsync()
        {
            if (await RegionDetector.IsInChinaAsync()) return;

            Dispatcher.UIThread.Post(() =>
            {
                PluginTypeItem.IsVisible = false;
                DllModTypeItem.IsVisible = false;

                if (_lastSearchType is SearchResourceType.PluginPack or SearchResourceType.DllMods)
                {
                    _lastSearchType = SearchResourceType.Unknow;
                    ResourceTypeBox.SelectedIndex = 0;
                }
            });
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

        public void OnSearch(SearchInfo info, bool saveHistory = true)
        {
            SaveSearchType(info.Type);
            if (saveHistory)
                SaveSearchHistory(info);

            if (_isSearching)
            {
                _pendingSearch = info;
                _pendingSearchSaveHistory = saveHistory;
                return;
            }

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
            ResultPage.IsVisible = false;
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
                var selectedClass = (CurseForgeClassBox.SelectedItem as ComboBoxItem)?.Tag as CurseForgeResponse.Category;
                var selectedCategory = (CurseForgeCategoryBox.SelectedItem as ComboBoxItem)?.Tag as CurseForgeResponse.Category;
                _currentSearch.SetExtraParameter(new CurseForgeSearch.CurseForgeSearchFilter(
                    selectedClass?.Id,
                    selectedCategory?.Id));
            }
        }

        private void PrepareSearchUI(SearchInfo info)
        {
            IsEdit = false;

            MinecraftTypePanel.IsVisible = info.Type == SearchResourceType.Minecraft;
            CurseForgeResTypePanel.IsVisible = info.Type == SearchResourceType.ResourcePack;

            if (info.Type != SearchResourceType.Unknow)
                ResourceTypeBox.SelectedIndex = (int)info.Type;
            else
                info.Type = (SearchResourceType)ResourceTypeBox.SelectedIndex;

            if (info.Type != SearchResourceType.ResourcePack)
            {
                _isUpdatingCurseForgeFilters = true;
                CurseForgeClassBox.SelectedIndex = 0;
                UpdateCurseForgeCategoryOptions();
                _isUpdatingCurseForgeFilters = false;
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

                if (_pendingSearch != null)
                {
                    var pendingSearch = _pendingSearch;
                    var saveHistory = _pendingSearchSaveHistory;
                    _pendingSearch = null;
                    _pendingSearchSaveHistory = true;
                    OnSearch(pendingSearch, saveHistory);
                    return;
                }

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

            if (ResultsMatch(_displayedResult, result) &&
                _displayedPage == _currentPage && _displayedTotalPages == _totalPages)
            {
                LoadingRing.IsVisible = false;
                ResultPage.IsVisible = result.Items.Count > 0;
                NoneBox.IsVisible = result.Items.Count == 0;
                return;
            }

            _displayedResult = result;
            _displayedPage = _currentPage;
            _displayedTotalPages = _totalPages;
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

        /// <summary>
        ///     从详情页返回时重建结果列表：进详情页会让本页脱离视觉树，
        ///     内部 NavigationFrame 卸载时自毁内容（分页文字保留，列表被清空），
        ///     这里用缓存的上一页结果原地重建，不重新请求网络。
        /// </summary>
        public void RestoreDisplayedResults()
        {
            if (_isSearching) return;
            if (_displayedResult == null || _displayedResult.Items.Count == 0) return;

            ResultPage.Update(CreateResultsView(_displayedResult.Items), _displayedTotalPages, _displayedPage);
        }

        private static bool ResultsMatch(SearchResultPage? current, SearchResultPage updated)
        {
            if (current == null || current.TotalCount != updated.TotalCount ||
                current.Items.Count != updated.Items.Count)
                return false;

            for (var i = 0; i < current.Items.Count; i++)
            {
                var left = current.Items[i];
                var right = updated.Items[i];
                if (left.Id != right.Id || left.Name != right.Name ||
                    left.Description != right.Description || left.IconUri != right.IconUri ||
                    left.DownloadCount != right.DownloadCount || left.DateUpdated != right.DateUpdated ||
                    left.DateCreated != right.DateCreated || left.SourceWebsite != right.SourceWebsite ||
                    left.ResourceType != right.ResourceType || left.DataType != right.DataType ||
                    left.JsonData != right.JsonData ||
                    !left.Authors.SequenceEqual(right.Authors) ||
                    !left.Labels.SequenceEqual(right.Labels) ||
                    !left.HeaderLabels.SequenceEqual(right.HeaderLabels) ||
                    !(left.Images ?? new List<string>()).SequenceEqual(right.Images ?? new List<string>()))
                    return false;
            }

            return true;
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
                Margin = new Thickness(20, 10, 5, 20),
                Padding = new Thickness(0,0,15,0),
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
                    new Setter(Layoutable.MinHeightProperty, 0d),
                    new Setter(Layoutable.MinWidthProperty, 0d),
                    new Setter(Layoutable.MarginProperty, new Thickness(0, 0, 0, 8)),

                    new Setter(
                        TemplatedControl.TemplateProperty,
                        new FuncControlTemplate<ListBoxItem>((item, ns) =>
                        {
                            var presenter = new ContentPresenter
                            {
                                Name = "PART_ContentPresenter",
                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                VerticalContentAlignment = VerticalAlignment.Center,
                            };

                            // 绑定到 ListBoxItem 的属性
                            presenter.Bind(ContentPresenter.ContentProperty,
                                item.GetObservable(ContentControl.ContentProperty));
                            presenter.Bind(ContentPresenter.ContentTemplateProperty,
                                item.GetObservable(ContentControl.ContentTemplateProperty));
                            presenter.Bind(ContentPresenter.PaddingProperty,
                                item.GetObservable(TemplatedControl.PaddingProperty));
                            presenter.Bind(ContentPresenter.BackgroundProperty,
                                item.GetObservable(TemplatedControl.BackgroundProperty));

                            return presenter;
                        }))
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
            {
                CurseForgeClassBox.SelectedIndex = 0;
                CurseForgeCategoryBox.SelectedIndex = 0;
            }

            OnSearch(SearchInfo);
        }

        private async Task LoadCurseForgeCategoriesAsync()
        {
            try
            {
                var api = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
                _curseForgeCategories = await api.GetCategoriesAsync();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _isUpdatingCurseForgeFilters = true;
                    foreach (var category in _curseForgeCategories.Where(x => x.IsClass).OrderBy(x => x.Name))
                        CurseForgeClassBox.Items.Add(new ComboBoxItem
                        {
                            Content = GetCurseForgeClassName(category),
                            Tag = category
                        });
                    CurseForgeClassBox.SelectedIndex = 0;
                    UpdateCurseForgeCategoryOptions();
                    _isUpdatingCurseForgeFilters = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载 CurseForge 分类失败: {ex.Message}");
            }
        }

        private static string GetCurseForgeClassName(CurseForgeResponse.Category category)
        {
            return CurseForgeClassNames.TryGetValue(category.Id, out var key)
                ? I18nManager.Instance[key]
                : category.Name;
        }

        private void UpdateCurseForgeCategoryOptions()
        {
            var selectedClass = CurseForgeClassBox.SelectedIndex > 0
                ? (CurseForgeClassBox.SelectedItem as ComboBoxItem)?.Tag as CurseForgeResponse.Category
                : null;
            CurseForgeCategoryBox.Items.Clear();
            var hasSelectedType = selectedClass != null;
            CurseForgeCategoryLabel.IsVisible = hasSelectedType;
            CurseForgeCategoryBox.IsVisible = hasSelectedType;
            if (!hasSelectedType)
            {
                CurseForgeCategoryBox.IsEnabled = false;
                CurseForgeCategoryBox.SelectedIndex = -1;
                return;
            }

            CurseForgeCategoryBox.Items.Add(new ComboBoxItem { Content = I18nManager.Instance["Download.CurseForge.Category.All"] });
            foreach (var category in _curseForgeCategories
                         .Where(x => !x.IsClass && x.ClassId == selectedClass!.Id && x.ParentCategoryId == selectedClass.Id)
                         .OrderBy(x => x.Name))
                    CurseForgeCategoryBox.Items.Add(new ComboBoxItem
                    {
                        Content = CurseForgeSearch.TranslateCategory(category),
                        Tag = category
                    });

            CurseForgeCategoryBox.SelectedIndex = 0;
            CurseForgeCategoryBox.IsEnabled = CurseForgeCategoryBox.ItemCount > 1;
        }

        private void SearchCurseForgeWithCurrentFilters()
        {
            if (!IsEdit || ResourceTypeBox.SelectedIndex != (int)SearchResourceType.ResourcePack)
                return;

            SearchInfo.Key = DownloadSearch.DownloadSearchView.SearchKey;
            SearchInfo.Type = SearchResourceType.ResourcePack;
            OnSearch(SearchInfo);
        }

        private void CurseForgeClassBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingCurseForgeFilters)
                return;

            _isUpdatingCurseForgeFilters = true;
            UpdateCurseForgeCategoryOptions();
            _isUpdatingCurseForgeFilters = false;
            SearchCurseForgeWithCurrentFilters();
        }

        private void CurseForgeCategoryBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingCurseForgeFilters)
                return;

            SearchCurseForgeWithCurrentFilters();
        }
    }
}
