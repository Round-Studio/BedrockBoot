using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Screenshots;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceScreenshots : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public InstanceScreenshots()
    {
        InitializeComponent();
    }

    public InstanceScreenshots(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        new ScreenshotsManager(versionInfo).GetScreenshots().Values.ToList()
            .ForEach(f => f.ForEach((f1) => Console.WriteLine(f1.FilePath)));
    }
}