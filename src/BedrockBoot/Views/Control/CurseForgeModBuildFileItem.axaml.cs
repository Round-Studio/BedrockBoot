using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;

namespace BedrockBoot.Views.Control;

public partial class CurseForgeModBuildFileItem : UserControl
{
    public CurseForgeResponse.ModFile ModFile { get; set; }
    public CurseForgeModBuildFileItem()
    {
        InitializeComponent();
    }
    public CurseForgeModBuildFileItem(CurseForgeResponse.ModFile modFile):this()
    {
        ModFile = modFile;

        Update();
    }

    private void Update()
    {
        Card.Header = ModFile.DisplayName;
        Card.Description = $"{ModFile.FileDate.ToShortDateString()} {ModFile.FileDate.ToShortTimeString()}";
    }
}