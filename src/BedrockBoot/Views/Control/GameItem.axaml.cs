using System;
using Windows.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockBoot.Views.Windows;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Control;

public partial class GameItem : UserControl
{
    public VersionConfig VersionInfo { get; set; }

    public GameItem()
    {
        InitializeComponent();
    }

    public GameItem(VersionConfig info) : this()
    {
        VersionInfo = info;
        
        Update();
    }

    public void Update()
    {
        VersionName.Text = VersionInfo.Info.VersionName;
        Card.Description = $"{VersionInfo.Info.VersionType}, {VersionInfo.Info.BuildType}, {VersionInfo.Info.Version}";

        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;
        
        var image = "avares://Round.Avalonia.Assets/Image/Icon/mc_grassblock_neo.png";
        if (VersionInfo.Info.VersionType != MinecraftGameTypeVersion.Release)
        {
            image = "avares://Round.Avalonia.Assets/Image/Icon/mc_soilblock_neo.png";
        }
        
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
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(VersionInfo),$"{VersionInfo.Info.VersionName} - {VersionInfo.Info.Version}");
    }
}