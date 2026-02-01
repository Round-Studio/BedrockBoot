using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.DownloadPage;
using BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchItem : UserControl
{
    private static readonly HttpClient _httpClient = new HttpClient();
    public SearchResultItemInfo SearchResultItemInfo { get; set; }
    
    public SearchItem()
    {
        InitializeComponent();
    }

    public SearchItem(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        ItemName.Text = info.Name;
        Description.Text = info.Description;
        Authors.Text = string.Join(", ", info.Authors);

        if (info.Labels.Count > 0)
        {
            LabelsPanel.IsVisible = true;
        }

        info.Labels.ForEach(s => LabelsPanel.Children.Add(new LabelBox() { Text = s }));

        Update();
    }

    private async Task Update()
    {
        var icon = await ImageLoader.LoadIconAsync(SearchResultItemInfo.IconUri);
        if (icon != null)
        {
            Card.IsFontIcon = false;
            Card.ImageIcon = icon;
        }
    }


    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        SearchResultItemInfo.OnClick?.Invoke(SearchResultItemInfo.JsonData);
    }
}