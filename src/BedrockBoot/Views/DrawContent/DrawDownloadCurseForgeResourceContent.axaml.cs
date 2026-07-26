using System;
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
	private ImageLoader _imageLoader = ImageLoader.Shared;
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

        ModData.Categories.ForEach(cat =>
        {
            TypesBox.Children.Add(new LabelBox
            {
                Text = cat.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2.5)
            });
        });

        _ = Task.Run(async () =>
        {
            try
            {
                var image = await _imageLoader.LoadImageBrushAsync(ModData.Logo.ThumbnailUrl);
                if (image == null) return;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    NullImage.IsVisible = false;
                    IconBox.Background = new ImageBrush
                    {
                        Source = image
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"加载 CurseForge 资源图标失败: {ex.Message}");
            }
        });
    }
}