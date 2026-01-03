using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

namespace BedrockBoot.Views.Control;

public partial class GameResourcePackItem : UserControl
{
    public ResourcePackManifest ResourcePackManifest { get; set; }
    public GameResourcePackItem()
    {
        InitializeComponent();
    }

    public GameResourcePackItem(ResourcePackManifest maf, bool isImport = false) : this()
    {
        ResourcePackManifest = maf;

        Update();
        ControlBox.IsVisible = !isImport;
    }

    public void Update()
    {
        Card.ImageIcon = new Bitmap(ResourcePackManifest.PackIcon!);
        PackName.MinecraftText = ResourcePackManifest.Header.Name;
        PackDescription.MinecraftText = ResourcePackManifest.Header.Description;
        PackType.Text = ResourcePackManifest.PackType.ToString();
    }
}