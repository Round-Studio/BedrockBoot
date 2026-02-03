using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameInstanceConfigContent : UserControl
{
    public string GameInstallFolder => GlobalModel.Config.Data.GameFolders[GameFolder.SelectedIndex].GameFolderPath;
    public string GameInstallName => GameName.Text;
    public DialogAddGameInstanceConfigContent()
    {
        InitializeComponent();
    }
    public DialogAddGameInstanceConfigContent(string packName):this()
    {
        GameName.Text = Path.GetFileName(packName).Replace(".mcpint", "");
        Update();
    }

    public void Update()
    {
        IsEnabled = false;

        GameFolder.Items.Clear();
        GlobalModel.Config.Data.GameFolders.ForEach(f =>
        {
            GameFolder.Items.Add(new ComboBoxItem
            {
                Content = $"{f.GameFolderName} - {f.GameFolderPath}"
            });
        });
        GameFolder.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;

        IsEnabled = true;
    }
}