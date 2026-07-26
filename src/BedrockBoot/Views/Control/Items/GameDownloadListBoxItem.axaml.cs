using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Control.Items;

public partial class GameDownloadListBoxItem : UserControl
{
    private BuildInfo? _buildInfo;

    public GameDownloadListBoxItem()
    {
        InitializeComponent();

        // 作为 DataTemplate 使用时，虚拟化回收/复用容器会重新赋 DataContext
        DataContextChanged += (_, _) =>
        {
            if (DataContext is BuildInfo info) Apply(info);
        };
    }

    public GameDownloadListBoxItem(BuildInfo buildInfo):this()
    {
        Apply(buildInfo);
    }

    private void Apply(BuildInfo buildInfo)
    {
        _buildInfo = buildInfo;
        VersionName.Text = buildInfo.ID;
        VersionD.Text = $"{buildInfo.BuildType}, {buildInfo.Type}, {buildInfo.Date}";
        IconBox.ImageUrl = buildInfo.Type == MinecraftGameTypeVersion.Release
            ? "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png"
            : "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";
    }
}