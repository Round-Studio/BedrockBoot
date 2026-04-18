using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Info.News;

namespace BedrockBoot.Views.Control.Items;

public partial class NewsItem : UserControl
{
    public NewsItem()
    {
        InitializeComponent();
    }

    public NewsItem(MojangNewsManifest.PatchNoteEntry info) : this()
    {
        ImageRender.Update("https://launchercontent.mojang.com" + info.Image.Url);
        ItemTitle.Text = info.Title;
        ItemSubTitle.Text = info.ShortText;
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        ImageRender.Effect = new BlurEffect
        {
            Radius = 20
        };
        ImageRender.Margin = new Thickness(-20);
    }


    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        ImageRender.Effect = new BlurEffect
        {
            Radius = 0
        };
        ImageRender.Margin = new Thickness(0);
    }
}