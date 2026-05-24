using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Views.Control.Items.System;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDropFileContent : UserControl
{
    private readonly IStorageItem[] _storageItems;

    public DrawDropFileContent()
    {
        InitializeComponent();
    }

    public DrawDropFileContent(IStorageItem[] storageItems) : this()
    {
        _storageItems = storageItems;
        UpdateUi();
    }

    public void UpdateUi()
    {
        FileCount.Text = $"已接收 {_storageItems.Length} 个文件";
        FilesPanel.Children.Clear();
        FilesPanel.Children.AddRange(_storageItems.Select(file => new DropFileItem(file)));
    }
}