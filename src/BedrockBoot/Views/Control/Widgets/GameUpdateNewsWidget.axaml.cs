using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Enum.News;
using BedrockBoot.Core.Models.News;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Control.Widgets;

public partial class GameUpdateNewsWidget : UserControl
{
    public GameUpdateNewsWidget()
    {
        InitializeComponent();
        Loaded += (sender, args) => Update();
    }

    public void Update()
    {
        Task.Run(async () =>
        {
            var lst = await NewsGenerate.GetPatchNotesAsync(SourceList.NewsUrl);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var itemIndex = 0; // 专门用于访问 lst 的索引
                for (var rowIndex = 0; rowIndex < lst.Count; rowIndex++)
                {
                    var line = NewsGenerate.GetRandomLine();

                    if (itemIndex + GetLineItemCount(line) > lst.Count)
                        break;

                    var columnDefs = new ColumnDefinitions();
                    foreach (var item in line)
                        switch (item)
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

                    var grid = new Grid
                    {
                        ColumnDefinitions = columnDefs,
                        ColumnSpacing = 8
                    };

                    var colIndex = 0;
                    foreach (var itemType in line)
                    {
                        if (itemIndex >= lst.Count) break;

                        var newItem = new NewsItem(lst[itemIndex]);
                        itemIndex++;

                        switch (itemType)
                        {
                            case NewsItemType.Big:
                                Grid.SetColumn(newItem, colIndex);
                                Grid.SetColumnSpan(newItem, 3);
                                grid.Children.Add(newItem);
                                colIndex += 3;
                                break;
                            case NewsItemType.Medium:
                                Grid.SetColumn(newItem, colIndex);
                                Grid.SetColumnSpan(newItem, 2);
                                grid.Children.Add(newItem);
                                colIndex += 2;
                                break;
                            case NewsItemType.Small:
                                Grid.SetColumn(newItem, colIndex);
                                Grid.SetColumnSpan(newItem, 1);
                                grid.Children.Add(newItem);
                                colIndex += 1;
                                break;
                        }
                    }

                    NewsList.Children.Add(grid);
                }

                LoadRing.IsVisible = false;
            });

            int GetLineItemCount(List<NewsItemType> line)
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
        });
    }
}