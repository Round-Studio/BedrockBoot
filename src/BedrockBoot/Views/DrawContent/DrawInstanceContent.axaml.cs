using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawInstanceContent : UserControl
{
    public DrawInstanceContent()
    {
        InitializeComponent();

        IsEditMode = true;

#if RELEASE
        GameControls.IsEnabled = GlobalModel.FunctionOption.IsEnableGameInstanceControl;
        GameModes.IsEnabled = GlobalModel.FunctionOption.IsEnableGameInstanceMods;
#endif
    }

    public DrawInstanceContent(VersionConfig info) : this()
    {
        VersionInfo = info;

        Update();
    }

    public VersionConfig VersionInfo { get; set; }
    public bool IsEditMode { get; set; }

    public void Update()
    {
        IsEditMode = false;

        var image = "avares://Round.SDK.Avalonia/Image/Icon/mc_grassblock_neo.png";
        if (VersionInfo.Info.VersionType != MinecraftGameTypeVersion.Release)
            image = "avares://Round.SDK.Avalonia/Image/Icon/mc_soilblock_neo.png";

        IconBox.Background = new ImageBrush
        {
            Source = GetImage(image)
        };

        InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
        VersionName.Text = VersionInfo.Info.VersionName;
        VersionReady.Text =
            $"{VersionInfo.Info.Version} · {VersionInfo.Info.VersionType} · {VersionInfo.Info.BuildType}";

        IsEditMode = true;
    }

    public Bitmap GetImage(string url)
    {
        var uri = new Uri(url);

        // 2. 使用 AssetLoader.Open 获取流
        using (var stream = AssetLoader.Open(uri))
        {
            // 3. 将流解码为 Bitmap
            return new Bitmap(stream);
        }
    }

    private void InstanceTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            var tag = ((TabItem)InstanceTabControl.SelectedItem).Tag.ToString();

            switch (tag)
            {
                case "Info":
                    InstanceFrame.NavigateTo(new InstanceInfo(VersionInfo));
                    break;
                case "Mods":
                    InstanceFrame.NavigateTo(new InstanceMods(VersionInfo));
                    break;
                case "Pack":
                    InstanceFrame.NavigateTo(new InstancePack(VersionInfo));
                    break;
                case "Save":
                    InstanceFrame.NavigateTo(new InstanceSave(VersionInfo));
                    break;
                case "Screenshots":
                    InstanceFrame.NavigateTo(new InstanceScreenshots(VersionInfo));
                    break;
                case "Server":
                    InstanceFrame.NavigateTo(new InstanceServer(VersionInfo));
                    break;
                case "Controls":
                    InstanceFrame.NavigateTo(new InstanceControls(VersionInfo));
                    break;
            }
        }
    }

    private void LaunchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskLaunchGameItem.Launch(VersionInfo);
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start("explorer", new[] { VersionInfo.VersionPath });
    }
}