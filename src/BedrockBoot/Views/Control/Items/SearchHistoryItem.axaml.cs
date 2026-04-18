using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchHistoryItem : UserControl
{
    private readonly SearchInfo _info;

    public SearchHistoryItem()
    {
        InitializeComponent();
    }
    
    public Action<SearchInfo>? SearchAction { get; set; }
    
    public SearchHistoryItem(SearchInfo info):this()
    {
        _info = info;
        SearchType.Text = info.Type switch
        {
            SearchResourceType.Minecraft => "Minecraft",
            SearchResourceType.ResourcePack => "资源包",
            _ => "未知"
        };
        SearchKey.Text = info.Key;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        SearchAction?.Invoke(_info);
    }
}