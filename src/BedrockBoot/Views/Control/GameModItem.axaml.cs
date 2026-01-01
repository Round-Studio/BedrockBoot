using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Control;

public partial class GameModItem : UserControl
{
    public ModInfo ModInfo { get; set; }

    public GameModItem()
    {
        InitializeComponent();
    }

    public GameModItem(ModInfo info) : this()
    {
        ModInfo = info;
        
        UpdateUI();
    }

    public void UpdateUI()
    {
        Card.Header = Path.GetFileName(ModInfo.File);
        Card.Description = $"{SizeHelper.FormatBytes(new FileInfo(ModInfo.File).Length)}，{ModInfo.InjectDelay} ms";
    }    
}