using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Pack.Game.Capes;
using BedrockBoot.Standard.Entity.Game.Pack.Capes;

namespace BedrockBoot.Views.DialogContent.Cape;

public partial class DialogAddCapeFileContent : UserControl
{
    public string CapeNameStr => CapeName.Text;
    public CapeFileType CapeFileType => (CapeFileType)CapeType.SelectedIndex;
    public DialogAddCapeFileContent()
    {
        InitializeComponent();
    }
    
    public DialogAddCapeFileContent(string capeFilePath) : this()
    {
        var icon = CapeImageHelper.PatchCapeIcon(capeFilePath);
        CapePreview.Source = icon;
    }
}