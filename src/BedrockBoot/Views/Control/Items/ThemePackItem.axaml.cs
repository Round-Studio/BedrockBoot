using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Pack.Theme;
using NotImplementedException = System.NotImplementedException;

namespace BedrockBoot.Views.Control.Items;

public partial class ThemePackItem : UserControl
{
    private readonly ThemePackManifest _manifest;

    public ThemePackItem()
    {
        InitializeComponent();
    }

    public ThemePackItem(ThemePackManifest manifest) : this()
    {
        _manifest = manifest;
        UpdaterUI();
    }

    private void UpdaterUI()
    {
        _ = ImageRenderWidget.Update(_manifest.PackIconFileName!);
        PackName.Text = _manifest.PackName;
        PackDescription.Text = _manifest.PackDescription;
    }
}