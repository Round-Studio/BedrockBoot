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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Enum.Search;
using BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadSearch : UserControl
{
    public static NavigationFrame? SearchFrame;
    public static SearchDetailed? SearchDetailed;
    public static DownloadSearch? DownloadSearchView;

    /// <summary>下载页内部页面切换通知（供 DownloadRoot 更新返回按钮）</summary>
    public static Action? InnerPageChanged;

    // 保存搜索状态
    private static string _lastSearchKey = string.Empty;
    private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
    private readonly DispatcherTimer _versionSearchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    public DownloadSearch()
    {
        InitializeComponent();

        DownloadSearchView = this;
        SearchFrame = NavigationFrame;

        // 恢复上次的搜索关键词
        if (!string.IsNullOrEmpty(_lastSearchKey)) KeyBox.Text = _lastSearchKey;

        // 延迟导航，确保UI已加载完成
        Dispatcher.UIThread.Post(() =>
        {
            NavigateInnerFrame(new SearchDefault());

            // 恢复上次搜索的关键词（详情页返回走实例复用，不经此路径）
            var restore = string.IsNullOrEmpty(_lastSearchKey)
                ? null
                : new SearchInfo { Key = _lastSearchKey, Type = _lastSearchType };

            if (restore == null) return;
            if (!EnsureSearchDetailed()) return;

            // 直接以关键词头部状态呈现，不播动画（页面刚创建）
            ActivateKeywordState(restore.Key, animate: false);
            SearchDetailed!.OnSearch(restore);
        });

        _versionSearchTimer.Tick += (_, _) =>
        {
            _versionSearchTimer.Stop();

            var key = KeyBox.Text?.Trim() ?? string.Empty;
            if (!Regex.IsMatch(key, @"^\d+(?:\.\d+)*\.?$") || !EnsureSearchDetailed())
                return;

            _lastSearchKey = key;
            _lastSearchType = SearchResourceType.Minecraft;
            SearchDetailed!.OnSearch(new SearchInfo
            {
                Key = key,
                Type = SearchResourceType.Minecraft
            }, saveHistory: false);
        };

        KeyBox.TextChanged += (_, _) =>
        {
            _versionSearchTimer.Stop();
            var key = KeyBox.Text?.Trim() ?? string.Empty;
            if (Regex.IsMatch(key, @"^\d+(?:\.\d+)*\.?$"))
                _versionSearchTimer.Start();
        };

        KeyBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Key.Enter) SearchBtn_OnClick(null, null);
        };
    }

    public string SearchKey => KeyBox.Text;

    /// <summary>
    ///     释放静态引用，供下载页卸载时调用，避免搜索结果等控件树常驻内存。
    ///     只在静态引用仍属于该实例时清空，防止清掉刚创建的新下载页。
    /// </summary>
    public static void Reset(DownloadSearch owner)
    {
        if (!ReferenceEquals(DownloadSearchView, owner)) return;

        SearchDetailed = null;
        DownloadSearchView = null;
        SearchFrame = null;
        InnerPageChanged = null;
    }

    /// <summary>下载页内部当前是否显示搜索结果页（而非下载首页）</summary>
    public static bool IsShowingDetailed => SearchFrame?.GetCurrentPage() is SearchDetailed;

    /// <summary>下载页内部切回下载首页，同时恢复搜索框（关键词头部复位）</summary>
    public static void ShowSearchDefault()
    {
        DownloadSearchView?.ResetKeywordStateCore();
        NavigateInnerFrame(new SearchDefault());
    }

    /// <summary>进入关键词头部状态：搜索框下滑隐藏，引号包裹的关键词上滑入位</summary>
    public static void ShowKeyword(string? keyword) =>
        DownloadSearchView?.ActivateKeywordState(keyword);

    /// <summary>从历史记录发起搜索：搜索框与关键词头部同步显示该关键词</summary>
    public static void SearchFromHistory(SearchInfo info)
    {
        if (DownloadSearchView == null) return;

        // 修复：查看历史记录时搜索框不显示关键词
        DownloadSearchView.KeyBox.Text = info.Key ?? string.Empty;
        _lastSearchKey = info.Key ?? string.Empty;
        _lastSearchType = info.Type;
        DownloadSearchView.ActivateKeywordState(info.Key);

        NavigateInnerFrame(new SearchDetailed(info));
    }

    private int _keywordStateGen;
    private const double KeywordHeaderHeight = 42;

    private void ActivateKeywordState(string? keyword, bool animate = true)
    {
        var gen = ++_keywordStateGen;
        KeywordText.Text = $"\"{keyword?.Trim()}\"";

        if (!animate && !string.IsNullOrEmpty(keyword?.Trim()))
        {
            KeywordText.Height = KeywordHeaderHeight;
            KeywordText.Opacity = 1;
            KeywordText.IsVisible = true;
            return;
        }
        else if (string.IsNullOrEmpty(keyword?.Trim()))
        {
            KeywordText.Height = 0;
            KeywordText.Opacity = 0;
            KeywordText.IsVisible = false;
            return;
        }

        KeywordText.IsVisible = true;
        // 等一帧让起始状态生效，再由 Transition 播放展开动画
        Dispatcher.UIThread.Post(() =>
        {
            if (gen != _keywordStateGen) return;
            KeywordText.Height = KeywordHeaderHeight;
            KeywordText.Opacity = 1;
        });
    }

    private async void ResetKeywordStateCore()
    {
        var gen = ++_keywordStateGen;
        if (!KeywordText.IsVisible) return;

        KeywordText.Height = 0;
        KeywordText.Opacity = 0;
        try { await Task.Delay(260); } catch { /* 动画等待被中断也无妨 */ }
        if (gen != _keywordStateGen) return;

        KeywordText.IsVisible = false;
    }

    /// <summary>统一切换下载页内部页面，并通知外部更新返回按钮</summary>
    public static void NavigateInnerFrame(object page)
    {
        SearchFrame?.NavigateTo(page);
        InnerPageChanged?.Invoke();
    }

    /// <summary>
    ///     全局搜索（Ctrl+K）入口：切到下载页并按默认分类规则发起聚合搜索。
    /// </summary>
    public static void SearchFromGlobal(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        key = key.Trim();

        MainPage.Instance?.SelectTagPage("main:download");

        var type = DownloadSearchView != null
            ? DownloadSearchView.Classify(key)
            : SearchResourceType.ResourcePack;

        _lastSearchKey = key;
        _lastSearchType = type;

        if (DownloadSearchView != null) DownloadSearchView.KeyBox.Text = key;

        DownloadSearchView?.ActivateKeywordState(key);

        NavigateInnerFrame(new SearchDetailed(new SearchInfo { Key = key, Type = type }));
    }

    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _versionSearchTimer.Stop();
        if (!EnsureSearchDetailed()) return;

        var searchKey = KeyBox.Text;
        var searchType = Classify(searchKey);

        // 保存搜索状态
        _lastSearchKey = searchKey;
        _lastSearchType = searchType;

        // 搜索框下滑隐藏，关键词头部上滑入位
        ActivateKeywordState(searchKey);

        SearchDetailed!.OnSearch(new SearchInfo
        {
            Key = searchKey,
            Type = searchType
        });
    }

    private bool EnsureSearchDetailed()
    {
        if (IsShowingDetailed) return true;

        NavigateInnerFrame(new SearchDetailed());
        return IsShowingDetailed;
    }

    public SearchResourceType Classify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return SearchResourceType.Minecraft;

        // 情况1：检查是否是版本号格式（支持多种格式）
        if (IsVersionNumber(text))
            return SearchResourceType.Minecraft;

        // 情况2：检查是否是带空格的数字序列
        if (IsNumberSequence(text))
            return SearchResourceType.Minecraft;

        if (SearchDetailed != null)
            if(SearchDetailed.ChooseType != _lastSearchType && 
               SearchDetailed.ChooseType != SearchResourceType.Minecraft)
                return SearchDetailed.ChooseType;

        return SearchResourceType.ResourcePack;
    }

    /// <summary>
    ///     判断是否为版本号格式
    /// </summary>
    private bool IsVersionNumber(string text)
    {
        // 常见的版本号格式：
        // 1. 纯数字版本：1, 1.0, 1.0.0, 1.0.0.0
        // 2. 带字母后缀：1.0a, 1.0-beta, 1.0.0-rc1
        // 3. 带字母前缀：v1.0, V2.1.3
        // 4. 带特殊分隔符：1_0, 1-0
        // 5. 小数点结尾 1.21.90.

        // 移除常见的版本前缀（如v、V）
        var processedText = text.Trim().ToLower();
        if (processedText.StartsWith("v"))
            processedText = processedText.Substring(1);

        // 移除小数点结尾
        if (processedText.EndsWith("."))
            processedText = processedText.Substring(0, processedText.Length - 1);



        // 模式1：基本版本号格式（数字.数字.数字...）
        // 例如：1.2.3, 0.1, 2024.12.31
        var versionPattern1 = @"^\d+(?:[\._-]\d+)*$";
        if (Regex.IsMatch(processedText, versionPattern1))
            return true;

        // 模式2：带字母或特殊字符的版本号
        // 例如：1.2.3-beta, 2.0a, 1.0-rc1, 3.4.5+build123
        var versionPattern2 = @"^\d+(?:[\._-]\d+)*(?:[\._-]?[a-zA-Z][a-zA-Z0-9]*)?(?:\+[a-zA-Z0-9]+)?$";
        if (Regex.IsMatch(processedText, versionPattern2))
            return true;

        return false;
    }

    /// <summary>
    ///     判断是否为带空格的数字序列
    /// </summary>
    private bool IsNumberSequence(string text)
    {
        // 模式：一个或多个数字，中间用空格分隔
        // 例如："1 2 3", "123 456 789", "1 2 1 23"

        // 去首尾空格
        var trimmedText = text.Trim();

        // 如果包含非数字和非空格字符，直接返回false
        foreach (var c in trimmedText)
            if (!char.IsDigit(c) && c != ' ')
                return false;

        // 按空格分割
        var parts = trimmedText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // 必须有至少两个数字部分才被认为是"带空格的数字序列"
        if (parts.Length < 2)
            return false;

        // 检查每个部分是否都是有效的数字
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            // 如果部分不是纯数字，返回false
            if (!IsPureDigits(part))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     检查字符串是否只包含数字
    /// </summary>
    private bool IsPureDigits(string text)
    {
        foreach (var c in text)
            if (!char.IsDigit(c))
                return false;
        return true;
    }
}