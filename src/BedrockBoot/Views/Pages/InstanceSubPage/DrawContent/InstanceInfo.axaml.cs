using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceInfo : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public InstanceInfo()
    {
        InitializeComponent();
    }

    public InstanceInfo(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
    }

    public void UpdateUI()
    {
        
    }
}