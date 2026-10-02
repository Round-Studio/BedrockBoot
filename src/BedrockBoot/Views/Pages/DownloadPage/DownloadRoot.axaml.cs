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

using Avalonia.Interactivity;
using BedrockBoot.Standard.Entity;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadRoot : BedrockBootPage
{
    private readonly DownloadSearch _searchPage;

    // 从搜索结果页进入详情页时记住搜索条件，返回时恢复结果列表
    private SearchInfo? _restoreSearchInfo;

    public DownloadRoot()
    {
        InitializeComponent();
        Instance = this;

        _searchPage = new DownloadSearch();
        MainFrame.NavigateTo(_searchPage);

        // 内部页面（下载首页 / 搜索结果页）切换时更新返回按钮
        DownloadSearch.InnerPageChanged = UpdateBackBtn;
        UpdateBackBtn();
    }

    public static DownloadRoot Instance { get; private set; }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // 离开下载页时释放内部页面树与静态引用，避免搜索结果、图片缓存等整棵控件树常驻内存
        DownloadSearch.InnerPageChanged = null;
        MainFrame.Clear();
        DownloadSearch.Reset(_searchPage);

#if WINDOWS
        // 离开下载页后整理一次工作集，把内存还给系统
        ProcessMemoryTrimmer.TrimProcess();
#endif
    }

    public void NavigateTo(object page)
    {
        var current = MainFrame.GetCurrentPage();

        // 从搜索结果页进详情页时记下条件，返回时能回到结果列表；其余情况不需要恢复
        _restoreSearchInfo = page is not DownloadSearch &&
                             current is DownloadSearch &&
                             DownloadSearch.IsShowingDetailed
            ? SearchDetailed.SearchInfo
            : null;

        MainFrame.NavigateTo(page);
        UpdateBackBtn();
    }

    /// <summary>返回上一级：搜索结果页 -> 下载首页；详情页 -> 下载页（按需恢复结果列表）</summary>
    public void GoBack()
    {
        var current = MainFrame.GetCurrentPage();

        if (current is DownloadSearch)
        {
            if (DownloadSearch.IsShowingDetailed) DownloadSearch.ShowSearchDefault();
            return;
        }

        if (current != null)
        {
            DownloadSearch.SetPendingRestore(_restoreSearchInfo);
            NavigateTo(new DownloadSearch());
        }
    }

    private void UpdateBackBtn()
    {
        var current = MainFrame.GetCurrentPage();
        BackBtn.IsVisible = current switch
        {
            DownloadSearch => DownloadSearch.IsShowingDetailed,
            null => false,
            _ => true
        };
    }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e) => GoBack();
}