using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstancePack : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public InstancePack()
    {
        InitializeComponent();
    }

    public InstancePack(VersionConfig versionConfig) : this()
    {
        VersionInfo = versionConfig;
        new ResourcePackManager(VersionInfo).GetAllPack().ForEach(x =>
        {
            if (x != null &&
                x.Header != null)
            {
                Console.WriteLine($"找到包：{x.Header.Name}");
                ResultBox.Children.Add(new GameResourcePackItem(x));
            }
        });
    }
}