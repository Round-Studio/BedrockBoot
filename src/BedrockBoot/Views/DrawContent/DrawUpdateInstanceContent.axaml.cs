using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Instance;
using BedrockBoot.Views.Pages.InstanceSubPage.UpdateContent;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawUpdateInstanceContent : UserControl
{
    private readonly VersionConfig _versionConfig;

    public DrawUpdateInstanceContent()
    {
        InitializeComponent();
    }

    public DrawUpdateInstanceContent(VersionConfig value) : this()
    {
        _versionConfig = value;
        UpdateUi();
    }

    public async void UpdateUi()
    {
        var download = new InstanceUpdater(_versionConfig)
        {
            ChooseDownloadUrl = (lst) => lst[0].Url
        };
        NavigationFrame.NavigateTo(new UpdateChooseVersion(download.GetUpdateableVersions()));
        LoadRing.IsVisible = false;
    }
}