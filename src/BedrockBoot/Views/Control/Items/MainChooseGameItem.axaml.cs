using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class MainChooseGameItem : UserControl
{
    private readonly VersionConfig _versionConfig;

    public MainChooseGameItem()
    {
        InitializeComponent();
    }
    public MainChooseGameItem(VersionConfig versionConfig):this()
    {
        _versionConfig = versionConfig;

        GameInfo.Text = $"{versionConfig.Info.VersionType} {versionConfig.Info.Version}";
        GameName.Text = versionConfig.Info.VersionName;
        GameBuildType.Text = versionConfig.Info.BuildType.ToString();
        GameIcon.Source = GetImage(IconHelper.GetGameIconUrl(versionConfig));
    }

    public Bitmap GetImage(string url)
    {
        var uri = new Uri(url);

        using (var stream = AssetLoader.Open(uri))
        {
            return new Bitmap(stream);
        }
    }
}