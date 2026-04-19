using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogImportGameContent : UserControl
{
    public bool IsGDK;

    public DialogImportGameContent()
    {
        InitializeComponent();

        GlobalModel.Config.Data.GameFolders.ForEach(f =>
            GameInstallFoldersInputBox.Items.Add($"[{f.GameFolderName}] {f.GameFolderPath}"));

        if (GlobalModel.Config.Data.GameFolders.Count > 0)
            GameInstallFoldersInputBox.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
    }

    public string PackFile => PathInputBox.Text;
    public string PackInstallName => NameInputBox.Text;
    public bool DontKnowGameType => (bool)DontKnowGameTypeCheckBox.IsChecked;
    public MinecraftGameTypeVersion GameType => (MinecraftGameTypeVersion)RealGameBuildTypeInputBox.SelectedIndex;

    public string PackInstallFolder => GlobalModel.Config.Data.GameFolders.Count != 0
        ? GlobalModel.Config.Data.GameFolders[GameInstallFoldersInputBox.SelectedIndex].GameFolderPath
        : string.Empty;

    private async void OpenChooseFileBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "请选择文件",
            AllowMultiple = false
        });

        if (files != null && files.Count >= 1)
        {
            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            if (File.Exists(filePath))
            {
                PathInputBox.Text = filePath;
                NameInputBox.Text = Path.GetFileName(filePath);
                var type = PackAnalysis.GetPackBuildTypeWithFileHeader(filePath);
                if (type == MinecraftBuildTypeVersion.GDK)
                {
                    GDKItem.IsVisible = true;
                    IsGDK = true;
                }
                else
                {
                    GDKItem.IsVisible = false;
                    IsGDK = false;
                }
            }
        }
    }
}