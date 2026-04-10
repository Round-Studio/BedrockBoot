using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;

namespace BedrockBoot.Views.Control.Items;

public partial class GameItem : UserControl
{
    public GameItem()
    {
        InitializeComponent();
    }

    public GameItem(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }

    public void Update()
    {
        VersionName.Text = VersionInfo.Info.VersionName;
        Card.Description = $"{VersionInfo.Info.VersionType}, {VersionInfo.Info.BuildType}, {VersionInfo.Info.Version}";

        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        var image = "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png";
        if (VersionInfo.Info.VersionType != MinecraftGameTypeVersion.Release)
            image = "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";

        Card.ImageIcon = GetImage(image);
    }

    public Bitmap GetImage(string url)
    {
        var uri = new Uri(url);

        using (var stream = AssetLoader.Open(uri))
        {
            return new Bitmap(stream);
        }
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskLaunchGameItem.Launch(VersionInfo);
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(VersionInfo),
            $"{VersionInfo.Info.VersionName} - {VersionInfo.Info.Version}");
    }
}