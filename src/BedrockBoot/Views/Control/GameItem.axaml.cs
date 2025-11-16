using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Views.Control;

public partial class GameItem : UserControl
{
    public VersionInfo VersionInfo { get; set; }

    public GameItem()
    {
        InitializeComponent();
    }

    public GameItem(VersionInfo info) : this()
    {
        VersionInfo = info;
        
        Update();
    }

    public void Update()
    {
        Card.Header = VersionInfo.VersionName;
        Card.Description = $"{VersionInfo.Type} {VersionInfo.RealVersion}";
    }
}