using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportModContent : UserControl
{
    public string ModFile => PathInputBox.Text;
    public int ModDelay => (int)InjectionDelay.Value;
    public DialogImportModContent()
    {
        InitializeComponent();
    }

    private async void OpenChooseFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "请选择 DLL 文件",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            {
                new FilePickerFileType("DLL 文件")
                {
                    Patterns = new[] { "*.dll" }
                }
            }
        });

        if (files != null && files.Count >= 1)
        {
            IStorageFile selectedFile = files[0];
            string filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath))
            {
                PathInputBox.Text = filePath;
            }
        }
    }
}