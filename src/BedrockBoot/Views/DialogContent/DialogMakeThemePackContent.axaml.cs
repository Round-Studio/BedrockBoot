using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Pack.Theme;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMakeThemePackContent : UserControl
{
    public DialogMakeThemePackContent()
    {
        InitializeComponent();
    }

    public ThemePackManifest Manifest => new()
    {
        PackAuthor = string.IsNullOrEmpty(ThemePackAuthor.Text) ? string.Empty : ThemePackAuthor.Text,
        PackName = string.IsNullOrEmpty(ThemePackName.Text) ? string.Empty : ThemePackName.Text,
        PackDescription = string.IsNullOrEmpty(ThemePackDescription.Text) ? string.Empty : ThemePackDescription.Text,
        PackIconFileName = string.IsNullOrEmpty(ThemePackIcon.Text) ? string.Empty : ThemePackIcon.Text
    };

    private async void ImportPackIconButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window == null) return;

        var fileTypes = new List<FilePickerFileType>
        {
            FilePickerFileTypes.ImageAll
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            FileTypeFilter = fileTypes,
            AllowMultiple = false
        });

        if (files != null && files.Count > 0)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;

            ThemePackIcon.Text = filePath;
        }
    }
}