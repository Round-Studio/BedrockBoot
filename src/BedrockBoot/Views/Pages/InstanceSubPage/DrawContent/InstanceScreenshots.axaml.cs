using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Screenshots;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceScreenshots : ISetting
{
    public VersionConfig VersionInfo { get; set; }

    public InstanceScreenshots()
    {
        InitializeComponent();
    }

    public InstanceScreenshots(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        UpdateUI();
    }

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

        if (screenshots.Count <= 0)
        {
            NullBox.IsVisible = true;
        }
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
            UpdateScreenshots();
    }
}