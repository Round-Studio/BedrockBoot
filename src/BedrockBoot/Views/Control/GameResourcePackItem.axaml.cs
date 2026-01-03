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
    public GameResourcePackItem(ResourcePackManifest maf):this()
    {
        ResourcePackManifest = maf;
        
        Update();
    }

    public void Update()
    {
        Card.ImageIcon = new Bitmap(Path.Combine(ResourcePackManifest.PackRootPath, "pack_icon.png"));
        PackName.MinecraftText = ResourcePackManifest.Header.Name;
        PackDescription.MinecraftText = ResourcePackManifest.Header.Description;
    }
}