using System.Drawing;
using System.Windows.Forms;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack;

namespace BedrockBoot.Win32.Controls;

public partial class PackItem : UserControl
{
    public PackItem(ResourcePackManifest maf)
    {
        InitializeComponent();

        PackName.Text = maf.Header.Name;
        PackDes.Text = maf.Header.Description;
        PackIcon.Image = new Bitmap(maf.PackIcon);
        PackIcon.SizeMode = PictureBoxSizeMode.StretchImage;
    }
}