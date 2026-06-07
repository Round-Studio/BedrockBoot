using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info.News;
using BedrockBoot.Base.Enum.News;
using BedrockBoot.Core.Models.News;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Control.Widgets;

public partial class GameUpdateNewsWidget : UserControl
{
    private CancellationTokenSource? _batchCts;

    public GameUpdateNewsWidget()
    {
        InitializeComponent();
        Loaded += (sender, args) => Update();
        Unloaded += (_, _) => _batchCts?.Cancel();
    }

    public void Update()
    {
        _batchCts?.Cancel();
        _batchCts = new CancellationTokenSource();
        var token = _batchCts.Token;

        // 后台只取数据 + 算好行布局（纯计算，不涉及 Avalonia 控件）
        Task.Run(async () =>
        {
            var lst = await NewsGenerate.GetPatchNotesAsync(SourceList.NewsUrl);
            if (token.IsCancellationRequested) return;

            var plans = BuildNewsRowPlans(lst);
            if (token.IsCancellationRequested) return;

            // 控件创建必须在 UI 线程，所以分批在 UI 线程上构造 Grid + NewsItem 并挂入
            const int batchSize = 4;
            for (var i = 0; i < plans.Count; i += batchSize)
            {
                if (token.IsCancellationRequested) return;
                var slice = plans.GetRange(i, Math.Min(batchSize, plans.Count - i));
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var plan in slice) NewsList.Children.Add(BuildRow(plan));
                });
                await Task.Delay(16, token);
            }

            await Dispatcher.UIThread.InvokeAsync(() => { LoadRing.IsVisible = false; });
        }, token);
    }

    /// <summary>
    /// 纯数据规划：在后台线程上算出每行应该有哪些条目及 span，零 Avalonia 依赖。
    /// </summary>
    private static List<RowPlan> BuildNewsRowPlans(List<MojangNewsManifest.PatchNoteEntry> lst)
    {
        var rows = new List<RowPlan>();
        if (lst == null || lst.Count == 0) return rows;

        var itemIndex = 0;
        while (itemIndex < lst.Count)
        {
            var line = NewsGenerate.GetRandomLine();
            if (itemIndex + GetLineItemCount(line) > lst.Count) break;

            var plan = new RowPlan { Items = new List<(MojangNewsManifest.PatchNoteEntry entry, NewsItemType type)>(line.Count) };
            foreach (var itemType in line)
            {
                if (itemIndex >= lst.Count) break;
                plan.Items.Add((lst[itemIndex], itemType));
                itemIndex++;
            }
            rows.Add(plan);
        }
        return rows;
    }

    /// <summary>
    /// 在 UI 线程上把规划渲染成可视 Grid；只能在此处创建 Avalonia 控件。
    /// </summary>
    private static Grid BuildRow(RowPlan plan)
    {
        var columnDefs = new ColumnDefinitions();
        foreach (var (_, type) in plan.Items)
        {
            switch (type)
            {
                case NewsItemType.Big:
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    break;
                case NewsItemType.Medium:
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    break;
                case NewsItemType.Small:
                    columnDefs.Add(new ColumnDefinition(GridLength.Star));
                    break;
            }
        }

        var grid = new Grid
        {
            ColumnDefinitions = columnDefs,
            ColumnSpacing = 8
        };

        var colIndex = 0;
        foreach (var (entry, type) in plan.Items)
        {
            var newItem = new NewsItem(entry);
            var span = type switch
            {
                NewsItemType.Big => 3,
                NewsItemType.Medium => 2,
                _ => 1
            };
            Grid.SetColumn(newItem, colIndex);
            Grid.SetColumnSpan(newItem, span);
            grid.Children.Add(newItem);
            colIndex += span;
        }

        return grid;
    }

    private sealed class RowPlan
    {
        public List<(MojangNewsManifest.PatchNoteEntry entry, NewsItemType type)> Items = new();
    }

    private static int GetLineItemCount(List<NewsItemType> line)
    {
        var count = 0;
        foreach (var item in line)
            switch (item)
            {
                case NewsItemType.Big:
                case NewsItemType.Medium:
                case NewsItemType.Small:
                    count++;
                    break;
            }
        return count;
    }
}
