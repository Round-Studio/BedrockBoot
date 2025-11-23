using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core.JsonHandle;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawDownloadGameContent : UserControl
{
    public VersionInformation VersionInformation { get; set; }

    public DrawDownloadGameContent()
    {
        InitializeComponent();
    }

    public DrawDownloadGameContent(VersionInformation info) : this()
    {
        VersionInformation = info;

        UpdateUI();
    }

    public void UpdateUI()
    {
        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
            InstallFolder.Items.Add($"[{folder.GameFolderName}] {folder.GameFolderPath}"));

        InstallFolder.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        InstallName.Text = VersionInformation.ID;
    }

    private void InstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TaskDownloadGameItem.Install(VersionInformation,
            GlobalModel.Config.Data.GameFolders[InstallFolder.SelectedIndex].GameFolderPath, InstallName.Text);
    }
}