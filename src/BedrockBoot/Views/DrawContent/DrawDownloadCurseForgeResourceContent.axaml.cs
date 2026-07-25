using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Pages.DownloadSubPage.CurseForge;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadCurseForgeResourceContent : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    public CurseForgeResponse.ModData ModData;

    public DrawDownloadCurseForgeResourceContent()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
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

        ModData.Categories.ForEach(cat =>
        {
            TypesBox.Children.Add(new LabelBox
            {
                Text = cat.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2.5)
            });
        });

        Task.Run(() =>
        {
            var image =_imageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl).Result;
            Dispatcher.UIThread.Invoke(() =>
            {
                NullImage.IsVisible = false;
                IconBox.Background = new ImageBrush
                {
                    Source = image
                };
            });
        });
    }
}