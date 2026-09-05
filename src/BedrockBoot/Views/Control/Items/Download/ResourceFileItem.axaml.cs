using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info.Download;
using NotImplementedException = System.NotImplementedException;

namespace BedrockBoot.Views.Control.Items.Download;

public partial class ResourceFileItem : UserControl
{
    private readonly ResourceFileInfo _info;

    public ResourceFileItem()
    {
        InitializeComponent();
    }

    public ResourceFileItem(ResourceFileInfo info) : this()
    {
        _info = info;
        UpdateUI();
    }


    private void UpdateUI()
    {
        Card.Header = _info.FileName;
        SaveBtn.IsVisible = _info.IsEnableSaveAs;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(_info.Description)) parts.Add(_info.Description);
        if (_info.FileSize > 0) parts.Add(ToFileSizeString(_info.FileSize));
        Card.Description = string.Join(", ", parts);
    }

    public static string ToFileSizeString(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private void DownloadBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _info.OnDownload?.Invoke(_info.FileName);
    }

    private void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _info.OnSaveAs?.Invoke(_info.FileName);
    }
}