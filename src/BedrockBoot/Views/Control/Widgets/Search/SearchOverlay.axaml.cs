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
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BedrockBoot.Models.Search;

namespace BedrockBoot.Views.Control.Widgets.Search;

public partial class SearchOverlay : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    private CancellationTokenSource? _searchCts;

    public SearchOverlay()
    {
        InitializeComponent();

        // TextBox 会吞掉方向键等按键，改为在容器上捕获（包含已处理事件）
        CardRoot.AddHandler(KeyDownEvent, Overlay_OnKeyDown, RoutingStrategies.Bubble, true);

        EmptyTitle.Text = i18n["Search.Empty.HintTitle"];
        EmptyMessage.Text = i18n["Search.Empty.HintMessage"];
    }

    /// <summary>打开搜索浮层，可携带预填关键词</summary>
    public async void Open(string? prefill = null)
    {
        if (IsVisible)
        {
            SearchBox.Focus();
            return;
        }

        Backdrop.Opacity = 0;
        CardRoot.Opacity = 0;
        CardRoot.RenderTransform = TransformOperations.Parse("scale(0.95)");

        IsVisible = true;

        // 等一帧让初始状态生效，随后由 Transition 播放进入动画
        await Dispatcher.UIThread.InvokeAsync(() => { });

        Backdrop.Opacity = 1;
        CardRoot.Opacity = 1;
        CardRoot.RenderTransform = TransformOperations.Parse("scale(1)");

        SearchBox.Text = prefill ?? "";
        ResultList.ItemsSource = null;
        ResultList.IsVisible = false;
        EmptyBox.IsVisible = true;

        SearchBox.Focus();
        SearchBox.CaretIndex = SearchBox.Text?.Length ?? 0;
    }

    /// <summary>关闭搜索浮层</summary>
    public async void Close()
    {
        if (!IsVisible) return;

        _searchCts?.Cancel();

        Backdrop.Opacity = 0;
        CardRoot.Opacity = 0;
        CardRoot.RenderTransform = TransformOperations.Parse("scale(0.95)");

        await Task.Delay(200);
        IsVisible = false;
    }

    private void Backdrop_OnPointerPressed(object? sender, PointerPressedEventArgs e) => Close();

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        _ = RunSearchAsync(_searchCts.Token);
    }

    private async Task RunSearchAsync(CancellationToken token)
    {
        try
        {
            // 输入防抖
            await Task.Delay(120, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var query = SearchBox.Text?.Trim() ?? string.Empty;

        List<SearchResult> results;
        try
        {
            results = await Task.Run(() => SearchEngine.Instance.Search(query), token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested) return;

        var isEmpty = results.Count == 0;
        ResultList.ItemsSource = results;
        ResultList.IsVisible = !isEmpty;
        EmptyBox.IsVisible = isEmpty;

        if (string.IsNullOrEmpty(query))
        {
            EmptyTitle.Text = i18n["Search.Empty.HintTitle"];
            EmptyMessage.Text = i18n["Search.Empty.HintMessage"];
        }
        else
        {
            EmptyTitle.Text = i18n["Search.Empty.Title"];
            EmptyMessage.Text = i18n["Search.Empty.Message"];
        }

        if (results.Count > 0) ResultList.SelectedIndex = 0;
    }

    private void Overlay_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                Close();
                break;
            case Key.Enter:
                e.Handled = true;
                ExecuteResult(ResultList.SelectedItem as SearchResult);
                break;
            case Key.Down:
                e.Handled = true;
                MoveSelection(1);
                break;
            case Key.Up:
                e.Handled = true;
                MoveSelection(-1);
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        if (ResultList.ItemsSource is not ICollection<SearchResult> items || items.Count == 0) return;

        var next = Math.Clamp(ResultList.SelectedIndex + delta, 0, items.Count - 1);
        ResultList.SelectedIndex = next;
        if (ResultList.SelectedItem != null) ResultList.ScrollIntoView(ResultList.SelectedItem);
    }

    private void ResultList_OnTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual visual) return;

        var hit = visual.GetSelfAndVisualAncestors()
            .OfType<Avalonia.Controls.Control>()
            .FirstOrDefault(c => c.DataContext is SearchResult);

        if (hit?.DataContext is SearchResult result) ExecuteResult(result);
    }

    private void ExecuteResult(SearchResult? result)
    {
        if (result == null) return;

        Close();

        try
        {
            result.Execute?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"执行搜索结果失败: {ex.Message}");
        }
    }
}
