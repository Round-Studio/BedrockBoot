using System.Collections.Generic;
using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DrawContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameFolders : ISettingPage
{
    public bool IsEdit;

    public GameFolders()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Folders.Title"]
            }
        };

        UpdateUI();
        IsEdit = true;
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();

        DialogHost.Show(new DialogInfo
        {
            Title = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.Title"],
            Content = dialog,
            CloseButtonText = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.Action"],
            SecondaryButtonText = I18nManager.Instance["MainWindow.Common.Cancel"],
            PrimaryButtonText = I18nManager.Instance["Setting.Game.Folders.Dialog.Add.ImportOther"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;

                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();

                    UpdateUI();
                }
            },
            PrimaryAction = () =>
            {
                Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawImportOtherLauncherContent(),
                    I18nManager.Instance["Setting.Game.Folders.Draw.Import.Title"]);
            }
        });
    }

    private void UpdateUI()
    {
        ListBox.Children.Clear();

        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            ListBox.Children.Add(new GameFolderSettingItem(folder, UpdateUI));
        });
    }
}