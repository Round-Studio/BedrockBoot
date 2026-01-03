using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control;

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
        PackTypeBox.IsVisible = false;
        Card.Description = $"{string.Join(", ", ModData.Authors.Select(x => x.Name))}, 下载量：{ModData.DownloadCount}";
        Task.Run(() =>
        {
            var image = GlobalModel.ImageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl).Result;
            Dispatcher.UIThread.Invoke(() =>
            {
                Card.IsFontIcon = false;
                Card.ImageIcon = image;
            });
        });
    }
}