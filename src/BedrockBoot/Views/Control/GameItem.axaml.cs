using System;
using Windows.Foundation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Windows;
using BedrockLauncher.Core;

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

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.BedrockCore.ChangeVersion(VersionInfo.Version_Path,new InstallCallback()
        {
            result_callback = new Action<AsyncStatus, Exception>((s,e) =>
            {
                
            }),
            registerProcess_percent = new Action<string, uint>((s, e) =>
            {
                
            })
        });
        GlobalModel.BedrockCore.LaunchGame(VersionType.Release);
    }

    private void Card_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.OpenDraw(new DrawInstanceContent(VersionInfo),$"{VersionInfo.VersionName} - {VersionInfo.RealVersion}");
    }
}