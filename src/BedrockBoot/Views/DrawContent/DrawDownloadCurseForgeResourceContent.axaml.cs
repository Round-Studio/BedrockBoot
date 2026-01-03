using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.DownloadSubPage.CurseForge;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadCurseForgeResourceContent : UserControl
{
    public CurseForgeResponse.ModData ModData;
    public DrawDownloadCurseForgeResourceContent()
    {
        InitializeComponent();
    }

    public DrawDownloadCurseForgeResourceContent(CurseForgeResponse.ModData mod) : this()
    {
        ModData = mod;
        Update();
    }

    public void Update()
    {
        PackName.Text = ModData.Name;
        PackDescription.Text = ModData.Summary;
        RankingBox.Text = ModData.GamePopularityRank.ToString();
        DownCountBox.Text = ModData.DownloadCount.ToString();
        InstanceFrame.NavigateTo(new CurseForgePackBuildFile(ModData));
        
        Task.Run(() =>
        {
            var image = GlobalModel.ImageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl).Result;
            Dispatcher.UIThread.Invoke(() =>
            {
                NullImage.IsVisible = false;
                IconBox.Background = new ImageBrush()
                {
                    Source = image as IImageBrushSource
                };
            });
        });
    }
}