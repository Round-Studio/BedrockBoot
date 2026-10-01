using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Xbox.Cape;

namespace BedrockBoot.Views.Control.Items.Xbox.Capes;

public partial class CapeItem : UserControl
{
    private readonly Cape _cape;

    public CapeItem()
    {
        InitializeComponent();
    }

    public CapeItem(Cape cape) : this()
    {
        _cape = cape;

        CapeImage.Update(cape.Thumbnail);
        CapeName.Text = cape.Title;
    }
}