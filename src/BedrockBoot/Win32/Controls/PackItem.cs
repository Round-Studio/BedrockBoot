using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;
using System.Drawing;
using System.Windows.Forms;

namespace BedrockBoot.Win32.Controls;

public partial class PackItem : UserControl
{
    public PackItem(ResourcePackManifest maf)
    {
        InitializeComponent();

        PackName.Text = maf.Header.Name;
        PackIcon.Image = new Bitmap(maf.PackIcon);
    }
}