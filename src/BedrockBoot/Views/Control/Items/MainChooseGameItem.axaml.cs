using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items;

public partial class MainChooseGameItem : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    private readonly VersionConfig _versionConfig;

    public MainChooseGameItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public MainChooseGameItem(VersionConfig versionConfig) : this()
    {
        _versionConfig = versionConfig;
        Update(_versionConfig);
    }

    public async Task Update(VersionConfig versionConfig)
    {
        GameInfo.Text = $"{versionConfig.Info.VersionType} {versionConfig.Info.Version}";
        GameName.Text = versionConfig.Info.VersionName;
        GameBuildType.Text = versionConfig.Info.BuildType.ToString();
        GameIcon.Source = await _imageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(versionConfig));
    }
}