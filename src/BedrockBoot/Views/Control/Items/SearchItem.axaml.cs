using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchItem : UserControl
{
    public SearchItem()
    {
        InitializeComponent();
    }

    public SearchItem(SearchResultItemInfo info) : this()
    {
        ItemName.Text = info.Name;
        Card.Description = info.Description;

        if (info.IconUri.StartsWith("avares://"))
        {
            Card.ImageIcon = new Bitmap(AssetLoader.Open(new Uri(info.IconUri)));
            Card.IsFontIcon = false;
        }
    }
}