using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportModContent : UserControl
{
    public DialogImportModContent()
    {
        InitializeComponent();
    }

    public string ModFile
    {
        get => PathInputBox.Text;
        set => PathInputBox.Text = value;
    }

    public int ModDelay
    {
        get => (int)InjectionDelay.Value;
        set => InjectionDelay.Value = value;
    }

    public bool IsPreLoad
    {
        get => EnablePreLoad.IsChecked ?? false;
        set => EnablePreLoad.IsChecked = value;
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
            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath)) PathInputBox.Text = filePath;
        }
    }
}