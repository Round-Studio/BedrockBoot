using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Control.Items;

public partial class GameDownloadListBoxItem : UserControl
{
    private readonly BuildInfo _buildInfo;

    public GameDownloadListBoxItem()
    {
        InitializeComponent();
    }
    public GameDownloadListBoxItem(BuildInfo buildInfo):this()
    {
        _buildInfo = buildInfo;
        VersionName.Text = buildInfo.ID;
        VersionD.Text = $"{buildInfo.BuildType}, {buildInfo.Type}, {buildInfo.Date}";
        IconBox.ImageUrl = buildInfo.Type == MinecraftGameTypeVersion.Release
            ? "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png"
            : "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";
    }
}