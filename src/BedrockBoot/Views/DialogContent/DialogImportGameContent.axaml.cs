using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportGameContent : UserControl
{
    public string PackFile => PathInputBox.Text;
    public string PackInstallName => NameInputBox.Text;

    public string PackInstallFolder =>
        GlobalModel.Config.Data.GameFolders[GameInstallFoldersInputBox.SelectedIndex].GameFolderPath;
    public DialogImportGameContent()
    {
        InitializeComponent();

        GlobalModel.Config.Data.GameFolders.ForEach(f => GameInstallFoldersInputBox.Items.Add($"[{f.GameFolderName}] {f.GameFolderPath}"));

        if (GlobalModel.Config.Data.GameFolders.Count > 0)
        {
            GameInstallFoldersInputBox.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        }
    }

    private async void OpenChooseFileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this); 

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "请选择文件",
            AllowMultiple = false,
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