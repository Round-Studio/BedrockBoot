using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Helper;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchItem : UserControl
{
    private static readonly HttpClient _httpClient = new();
    private ImageLoader _imageLoader = new ImageLoader();
    public SearchItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public SearchItem(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        ItemName.Text = info.Name;
        Description.Text = info.Description;
        Authors.Text = string.Join(", ", info.Authors);

        if (info.Labels.Count > 0) LabelsPanel.IsVisible = true;

        info.Labels.ForEach(s => LabelsPanel.Children.Add(new LabelBox { Text = s }));

        Update();
    }

    public SearchResultItemInfo SearchResultItemInfo { get; set; }

    private async Task Update()
    {
        var icon = await _imageLoader.LoadImageBrushAsync(SearchResultItemInfo.IconUri);
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