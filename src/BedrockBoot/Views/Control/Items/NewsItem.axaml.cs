using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BedrockBoot.Base.Entry.Info.News;

namespace BedrockBoot.Views.Control.Items;

public partial class NewsItem : UserControl
{
    private static readonly BlurEffect SharedBlur = new() { Radius = 8 };

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
        ImageRender.Effect = SharedBlur;
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        ImageRender.Effect = null;
    }
}
