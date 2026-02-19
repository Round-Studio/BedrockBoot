using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Widgets;

public partial class LocalImageRenderWidget : UserControl
{
    public LocalImageRenderWidget()
    {
        InitializeComponent();
    }

    public LocalImageRenderWidget(string uri) : this()
    {
        Update(uri);
    }

    public async Task Update(string uri)
    {
        var iamge = await ImageLoader.LoadIconAsync(uri);
        if (iamge != null) ImageBox.Background = new ImageBrush(iamge)
        {
            Stretch = Stretch.UniformToFill
        };
    }
}