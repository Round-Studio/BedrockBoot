using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Screenshots;
using BedrockBoot.Views.Control.Items;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceScreenshots : ISetting
{
    public InstanceScreenshots()
    {
        InitializeComponent();
    }

    public InstanceScreenshots(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

    public VersionConfig VersionInfo { get; set; }

    public void UpdateUI()
    {
        IsEdit = false;
        UserChooseBox.Items.Clear();
        var users = new ScreenshotsManager(VersionInfo).GetScreenshots();
        users.Keys.ToList().ForEach(u => UserChooseBox.Items.Add(u));
        UserChooseBox.SelectedIndex = 0;
        UpdateScreenshots();

        IsEdit = true;
    }

    public void UpdateScreenshots()
    {
        if (UserChooseBox.SelectedIndex <= -1)
            return;
        var users = new ScreenshotsManager(VersionInfo).GetScreenshots();
        var screenshots = users.Values.ToList()[UserChooseBox.SelectedIndex];
        ScreenshotsBox.Children.Clear();
        screenshots.ForEach(ph => ScreenshotsBox.Children.Add(new ScreenshotsItem(ph)));
        NullBox.IsVisible = false;

        if (screenshots.Count <= 0) NullBox.IsVisible = true;
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
            UpdateScreenshots();
    }

    private void OpenFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var paths = new ScreenshotsManager(VersionInfo).GetInstanceScreenshotsPath();
        var path = paths.Values.ToList()[UserChooseBox.SelectedIndex];

        OpenFolderHelper.Open(path);
    }
}