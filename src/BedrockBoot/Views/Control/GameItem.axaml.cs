using System;
using Windows.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
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
        Card.Header = VersionInfo.Info.VersionName;
        Card.Description = $"{VersionInfo.Info.VersionType}, {VersionInfo.Info.Version}";
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogLaunchGameContent.Launch(VersionInfo);
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(VersionInfo),$"{VersionInfo.Info.VersionName} - {VersionInfo.Info.Version}");
    }
}