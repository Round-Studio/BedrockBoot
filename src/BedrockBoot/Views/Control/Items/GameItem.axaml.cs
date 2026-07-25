using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Control.Items;

public partial class GameItem : UserControl
{
	private ImageLoader _imageLoader = new ImageLoader();
    public GameItem()
    {
        InitializeComponent();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
	    base.OnUnloaded(e);
	    _imageLoader.Dispose();
    }

    public GameItem(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }

    public async Task Update()
    {
        VersionName.Text = VersionInfo.Info.VersionName;
        Card.Description = $"{VersionInfo.Info.VersionType}, {VersionInfo.Info.BuildType}, {VersionInfo.Info.Version}";

        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        Card.ImageIcon = await _imageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(VersionInfo));
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