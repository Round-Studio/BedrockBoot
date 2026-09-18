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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;

namespace BedrockBoot.Views.Pages.DownloadSubPage;

public partial class DownloadAssetsPage : UserControl
{
    private readonly CurseForgeApiClient _apiClient;
    private readonly int _pageSize = 20;
    private int _currentIndex;

    // 添加分页相关字段
    private int _currentPage = 1;

    // 添加搜索状态
    private bool _isSearching;
    private int _totalPages;

    public DownloadAssetsPage()
    {
        InitializeComponent();
        _apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);

        // 绑定回车键搜索
        TextBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) Search();
        };

        Search();

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

    public string Key => TextBox.Text!;

    /// <summary>
    ///     跳转到指定页码
    /// </summary>
    private void GoToPage(int pageNumber)
    {
        if (_isSearching) return;

        _currentPage = Math.Clamp(pageNumber, 1, _totalPages);
        _currentIndex = (_currentPage - 1) * _pageSize;

        SearchWithPagination();
    }

    /// <summary>
    ///     搜索（重置到第一页）
    /// </summary>
    public void Search()
    {
        // 重置分页状态
        _currentPage = 1;
        _currentIndex = 0;

        SearchWithPagination();
    }

    /// <summary>
    ///     带分页的搜索
    /// </summary>
    private void SearchWithPagination()
    {
        if (_isSearching) return;

        var key = Key;

        _isSearching = true;
        ResultPage.CleanPage();
        NoneBox.IsVisible = false;
        LoadingRing.IsVisible = true;

        Task.Run(async () =>
        {
            try
            {
                var items = await _apiClient.SearchModsAsync(key, pageSize: _pageSize, index: _currentIndex);

                Dispatcher.UIThread.Invoke(() =>
                {
                    if (items?.Data?.Count > 0)
                    {
                        // 更新总页数
                        _totalPages =
                            (int)Math.Ceiling((double)items.Pagination.TotalCount / items.Pagination.PageSize);

                        ResultPage.Update(
                            new DownloadAssetsResultPage(items.Data),
                            _totalPages,
                            _currentPage);
                    }
                    else
                    {
                        NoneBox.IsVisible = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    NoneBox.IsVisible = true;
                    Console.WriteLine($@"搜索失败: {ex}");
                });
            }
            finally
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LoadingRing.IsVisible = false;
                    _isSearching = false;
                });
            }
        });
    }

    /// <summary>
    ///     点击搜索按钮
    /// </summary>
    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Search();
    }
}