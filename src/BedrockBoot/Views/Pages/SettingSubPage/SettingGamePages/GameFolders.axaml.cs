using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
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
    public GameFolders()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "游戏",
                ItemClickAction = (s) => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "实例目录"
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
            Title = "添加游戏根目录",
            Content = dialog,
            CloseButtonText = "添加",
            SecondaryButtonText = "取消",
            PrimaryButtonText = "导入其他启动器配置",
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
                GlobalModel.MainWindow.OpenDraw(new DrawImportOtherLauncherContent(), "导入其他启动器目录");
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