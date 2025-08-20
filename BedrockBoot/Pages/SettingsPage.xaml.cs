using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Pages.SettingPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public int DownThread = global_cfg.cfg.JsonCfg.DownThread;
    public int DelayTimes = global_cfg.cfg.JsonCfg.DelayTimes;
    public SettingsPage()
    {
        InitializeComponent();
        DownloadThreads.Text = DownThread.ToString();
        DelayTime.Text = DelayTimes.ToString();
        Unloaded += SettingsPage_Unloaded;
        SavaAppx.IsOn = global_cfg.cfg.JsonCfg.SaveAppx;
    

        UpdateUI();
    }
    public bool IsEdit = false;
    public void UpdateUI()
    {
        IsEdit = false;
        GameFoldersChooseBox.Items.Clear();
        global_cfg.cfg.JsonCfg.GameFolders.ForEach(x =>
        {
            GameFoldersChooseBox.Items.Add(new ComboBoxItem()
            {
                Content = $"{x.Name} - {Path.GetFullPath(x.Path)}"
            });
        });
        GameFoldersChooseBox.SelectedIndex = global_cfg.cfg.JsonCfg.ChooseFolderIndex;
        IsEdit = true;
    }
    private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            DownThread = int.Parse(DownloadThreads.Text);
            DelayTimes = int.Parse(DelayTime.Text);
            global_cfg.cfg.JsonCfg.SaveAppx = SavaAppx.IsOn;
            global_cfg.cfg.JsonCfg.DownThread = DownThread;
            global_cfg.cfg.JsonCfg.DelayTimes = DelayTimes;
            global_cfg.cfg.SaveConfig();
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    // TODO: 所以什么时候才能写SettingsCard导航？----DM,马上马上
    // BYD,我写了

    private void NavigationTo(SettingsCard sender)
    {
        var Type = sender.Tag as string;
        switch (Type)
        {
            case "Appearance":
                Frame.Navigate(typeof(AppearancePage));
                break;
            case "Behavior":
                Frame.Navigate(typeof(BehaviorPage));
                break;
            case "About":
                Frame.Navigate(typeof(AboutPage));
                break;
            case "DevTools":
                Frame.Navigate(typeof(DevelopersPage));
                break;
            default:
                throw new ArgumentException("Unknown settings card type: " + Type);
        }
    }

    private async void ImportGameFolder_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog();

        // 如果 ContentDialog 在桌面应用程序中运行，则必须设置 XamlRoot
        dialog.XamlRoot = this.Content.XamlRoot;
        // dialog.Background = new SolidColorBrush(Colors.Transparent);
        dialog.Content = new AddNewGameFolderContent();
        dialog.XamlRoot = this.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "新增游戏目录";
        dialog.PrimaryButtonText = "新增";
        dialog.CloseButtonText = "取消";
        dialog.DefaultButton = ContentDialogButton.Primary;

        var result = await dialog.ShowAsync();
        if(result == ContentDialogResult.Primary)
        {
            var folderpath = ((AddNewGameFolderContent)dialog.Content).FolderPath;
            var foldername = ((AddNewGameFolderContent)dialog.Content).FolderName;
            if (!string.IsNullOrEmpty(folderpath))
            {
                global_cfg.cfg.JsonCfg.GameFolders.Add(new Models.Entry.GameFolderInfoEntry()
                {
                    Name = foldername,
                    Path = folderpath,
                });
                global_cfg.cfg.SaveConfig();
                if(!Directory.Exists(folderpath))
                    {
                    Directory.CreateDirectory(folderpath);
                }
                UpdateUI();
            }
        }
    }

    private void GameFoldersChooseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            global_cfg.cfg.JsonCfg.ChooseFolderIndex = GameFoldersChooseBox.SelectedIndex;
            global_cfg.cfg.SaveConfig();
        }
    }
}
