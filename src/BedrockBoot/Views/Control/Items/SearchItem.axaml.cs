using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class SearchItem : UserControl
{
    private static readonly HttpClient _httpClient = new();

    public SearchItem()
    {
        InitializeComponent();
    }

    public SearchItem(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        ItemName.Text = info.Name;
        Description.Text = info.Description;

        if (info.Labels.Count > 0) LabelsPanel.IsVisible = true;

        info.Labels.ForEach(s => LabelsPanel.Children.Add(new LabelBox { Text = s }));

        LoadIconAsync(info.IconUri);
    }

    public SearchResultItemInfo SearchResultItemInfo { get; set; }

    private async void LoadIconAsync(string iconUri)
    {
        if (iconUri.StartsWith("avares://"))
        {
            // 处理本地资源
            Card.ImageIcon = new Bitmap(AssetLoader.Open(new Uri(iconUri)));
            Card.IsFontIcon = false;
        }
        else if (iconUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 iconUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // 处理网络图片
            await LoadNetworkImageAsync(iconUri);
        }
    }

    private async Task LoadNetworkImageAsync(string url)
    {
        try
        {
            // 下载图片数据
            var imageBytes = await _httpClient.GetByteArrayAsync(url);

            // 创建内存流
            using (var memoryStream = new MemoryStream(imageBytes))
            {
                // 创建Bitmap
                var bitmap = new Bitmap(memoryStream);

                // 需要在UI线程设置图片
                Dispatcher.UIThread.Post(() =>
                {
                    Card.ImageIcon = bitmap;
                    Card.IsFontIcon = false;
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载网络图片失败: {ex.Message}");
        }
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        SearchResultItemInfo.OnClick?.Invoke(SearchResultItemInfo.JsonData);
    }
}