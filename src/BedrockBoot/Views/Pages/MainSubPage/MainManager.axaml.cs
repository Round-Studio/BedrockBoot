using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainManager : UserControl
{
    public bool IsEditMode { get; set; } = false;
    public MainManager()
    {
        InitializeComponent();
        UpdateUI();
    }

    public void UpdateUI()
    {
        IsEditMode = false;
        
        if (GlobalModel.Config.Data.GameFolders.Count <= 0)
        {
            FolderList.IsVisible = false;
            FolderNull.IsVisible = true;
        }
        FolderList.IsVisible = true;
        FolderNull.IsVisible = false;

        FolderList.SelectedIndex = -1;
        FolderList.Items.Clear();
        
        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            FolderList.Items.Add(new ListBoxItem()
            {
                Content = new StackPanel()
                {
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = folder.GameFolderName,
                            Margin = new Thickness(5, 2, 5, 5),
                            FontSize = 16
                        },
                        new TextBlock()
                        {
                            Foreground = Brushes.Gray,
                            Text = folder.GameFolderPath,
                            Margin = new Thickness(5, 0, 5, 0),
                            FontSize = 9,
                            TextWrapping = TextWrapping.WrapWithOverflow
                        }
                    }
                },
                VerticalAlignment = VerticalAlignment.Center
            });
        });
        if (GlobalModel.Config.Data.GameFolders.Count == 1)
            FolderList.SelectedIndex = 0;
        else
            FolderList.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        
        UpdateGameList();
        
        IsEditMode = true;
    }

    public void UpdateGameList()
    {
        if (!Directory.Exists(Path.Combine(
                GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath,
                "bedrock_versions")))
        {
            GamesNull.IsVisible = true;
            GameScro.IsVisible = false;
            
            return;
        }
        
        var lst = Directory.GetDirectories(Path.Combine(
            GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex].GameFolderPath,
            "bedrock_versions"));
        
        GameList.Children.Clear();

        if (lst.Length > 0)
        {
            GamesNull.IsVisible = false;
            GameScro.IsVisible = true;
        }
        else
        {
            GamesNull.IsVisible = true;
            GameScro.IsVisible = false;
        }

        lst.ToList().ForEach(x =>
        {
            try
            {
                var info = GameInfoHelper.GetVersionInfo(x);
                Console.WriteLine(info.VersionName);

                GameList.Children.Add(new GameItem(info));
            }
            catch
            { }
        });
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
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
                    GlobalModel.Config.Save();
                    
                    UpdateUI();
                }
            }
        });
    }

    private void FolderList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode)
        {
            GlobalModel.Config.Data.GameFolderSelIndex = FolderList.SelectedIndex;
            GlobalModel.Config.Save();
            
            UpdateGameList();
        }
    }
}