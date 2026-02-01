using System.Collections.Generic;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.DialogContent;

public class DialogChooseGameContent : ISetting
{
    private List<VersionConfig>? Versions;

    public DialogChooseGameContent()
    {
        InitializeComponent();

        Update();
    }

    public VersionConfig VersionConfig => Versions[GameInstance.SelectedIndex];

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
        UpdateList();

        IsEnabled = true;
    }

    public void UpdateList()
    {
        GameInstance.Items.Clear();
        var index = GameFolder.SelectedIndex;
        var path = GlobalModel.Config.Data.GameFolders[index].GameFolderPath;
        Versions = GameInfoHelper.GetVersionConfigs(path);
        Versions.ForEach(v =>
        {
            GameInstance.Items.Add(new ComboBoxItem
            {
                Content = $"{v.Info.VersionName} - {v.Info.Version}",
                Tag = v
            });
        });
        GameInstance.SelectedIndex = GlobalModel.Config.Data.GameFolders[index].GameSelIndex;
    }

    private void GameFolder_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateList();
    }
}