using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadGameContent : UserControl
{
    public BuildInfo BuildInfo { get; set; }

    public DrawDownloadGameContent()
    {
        InitializeComponent();
    }

    public DrawDownloadGameContent(BuildInfo info) : this()
    {
        BuildInfo = info;

        UpdateUI();
    }

    public void UpdateUI()
    {
        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
            InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}"));

        InstallFolder.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        InstallName.Text = BuildInfo.ID;
    }

    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (InstallFolder.Items.Count <= 0)
        {
            var dialog = new DialogAddGameFolderContent();
         
            DialogHost.Show(new DialogInfo()
            {
                Title = "Add Game Folder",
                Content = dialog,
                CloseButtonText = "添加",
                SecondaryButtonText = "取消",
                AccountButton = DialogButtons.CloseButton,
                CloseAction = () =>
                {
                    if (Directory.Exists(dialog.FolderPath))
                    {
                        var name = string.IsNullOrEmpty(dialog.FolderName)
                            ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                            : dialog.FolderName;
                    
                        GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo()
                        {
                            GameFolderPath = dialog.FolderPath,
                            GameFolderName = name
                        });
                        GlobalModel.Config.Data.GameFolderSelIndex = 0;
                        GlobalModel.Config.Save();
                    
                        UpdateUI();
                        
                        TaskDownloadGameItem.Install(BuildInfo,
                            GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath, InstallName.Text);
        
                        GlobalModel.MainWindow.CloseDraw();
                    }
                }
            });
        }
        else
        {
            TaskDownloadGameItem.Install(BuildInfo,
                GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath, InstallName.Text);
        
            GlobalModel.MainWindow.CloseDraw();
        }
    }
}