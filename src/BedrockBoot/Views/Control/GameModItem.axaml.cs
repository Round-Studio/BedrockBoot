using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Mods;

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
        Card.Description = $"{FormatBytes(new FileInfo(ModInfo.File).Length)}，{ModInfo.InjectDelay} ms";
    }    
    static string FormatBytes(double bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        double number = bytes;

        while (number >= 1024 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:F1} {suffixes[counter]}";
    }
}