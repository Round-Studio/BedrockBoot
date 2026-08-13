using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Round.SDK.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogCreatePluginProjectContent : UserControl
{
    public PackConfig PackConfig => new PackConfig
    {
        PackName = string.IsNullOrWhiteSpace(PluginName.Text) ? "MyPlugin" : PluginName.Text.Trim(),
        PackAuthor = string.IsNullOrWhiteSpace(PluginAuthor.Text) ? "<unknown>" : PluginAuthor.Text.Trim(),
        PackDescription = string.IsNullOrEmpty(PluginDesc.Text) ? "这是一个插件" : PluginDesc.Text.Trim(),
        PackIconPath = PluginLogoBox.Text?.Trim() ?? "",
        PackFolder = ProjectPath
    };

    public string ProjectPath => PluginPathInputBox.Text?.Trim() ?? "";

    public DialogCreatePluginProjectContent()
    {
        InitializeComponent();
    }

    private async void OpenChooseFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择项目保存路径",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            PluginPathInputBox.Text = folders[0].Path.LocalPath;
        }
    }

    private async void OpenFileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择插件图标",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.ico", "*.svg" }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            PluginLogoBox.Text = files[0].Path.LocalPath;
        }
    }
}