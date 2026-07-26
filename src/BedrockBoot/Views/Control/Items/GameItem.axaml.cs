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
	private ImageLoader _imageLoader = ImageLoader.Shared;
    public GameItem()
    {
        InitializeComponent();

        // 作为 DataTemplate 使用时，容器被虚拟化回收/复用会重新赋 DataContext，
        // 此处据此刷新内容，使同一个控件实例可以承载不同的实例数据。
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not VersionConfig config) return;

        VersionInfo = config;
        _ = Update();
    }

    public GameItem(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }

    public async Task Update()
    {
        if (VersionInfo == null) return;

        VersionName.Text = VersionInfo.Info.VersionName;
        Card.Description = $"{VersionInfo.Info.VersionType}, {VersionInfo.Info.BuildType}, {VersionInfo.Info.Version}";

        // 控件复用时需要显式复位，否则会残留上一条数据的状态
        EditModule.IsVisible = VersionInfo.Config.IsEditModel;

        // 记录当前请求对应的实例，避免图片异步返回时控件已被复用给另一条数据
        var requested = VersionInfo;
        var icon = await _imageLoader.LoadIconAsync(IconHelper.GetGameIconUrl(requested));
        if (!ReferenceEquals(requested, VersionInfo)) return;

        Card.ImageIcon = icon;
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