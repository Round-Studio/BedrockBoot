using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
}