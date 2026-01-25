using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Control.Items;

public partial class CurseForgeModItem : UserControl
{
    public CurseForgeResponse.ModData ModData { get; set; }
    public CurseForgeModItem()
    {
        InitializeComponent();
    }
    public CurseForgeModItem(CurseForgeResponse.ModData modData):this()
    {
        ModData = modData;

        Update();
    }

    public async Task Update()
    {
        PackName.Text = ModData.Name;
        Card.Description = $"{string.Join(", ", ModData.Authors.Select(x => x.Name))}, 下载量：{ModData.DownloadCount}";
        ModData.Categories.ForEach(cat =>
        {
            HeaderBox.Children.Add(new LabelBox()
            {
                Text = cat.Name,
                VerticalAlignment = VerticalAlignment.Center
            });
        });
        Task.Run(() =>
        {
            var image = GlobalModel.ImageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl).Result;
            if (image != null)
                Dispatcher.UIThread.Invoke(() =>
                {
                    Card.IsFontIcon = false;
                    Card.ImageIcon = image;
                });
        });
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawDownloadCurseForgeResourceContent(ModData),$"下载资源 {ModData.Name}");
    }
}