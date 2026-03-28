using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game.Pack.Archive;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelEditor;

public partial class LevelEditorRoot : UserControl
{
    public LevelEditorRoot()
    {
        InitializeComponent();
    }
    public LevelEditorRoot(ArchiveInfo info):this()
    {
        _info = info;
        UpdaterUI();
    }

    private void UpdaterUI()
    {
        LevelName.Text = _info.Name;
    }

    private ArchiveInfo _info;
    public Action? BackAction { get; set; }

    private void BackBtn_OnClick(object? sender, RoutedEventArgs e) => BackAction?.Invoke();
}