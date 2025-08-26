using ABI.System;
using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Models.Classes.Helper;
using BedrockBoot.Models.Classes.Style.Background;
using BedrockBoot.Models.Enum.Background;
using BedrockBoot.Tools;
using BedrockLauncher.Core;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
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
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public static MouseLocker Locker = new MouseLocker();
    public static bool IsLock = false;

    private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        App.Current.AppThemeService.OnThemeComboBoxSelectionChanged(sender);
    }
    private void cmbBackdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        App.Current.AppThemeService.OnBackdropComboBoxSelectionChanged(sender);
    }

    public SettingsPage()
    {
        InitializeComponent();

        App.Current.AppThemeService.SetThemeComboBoxDefaultItem(cmbTheme);
        App.Current.AppThemeService.SetBackdropComboBoxDefaultItem(cmbBackdrop);

        DownloadThreads.Value = DownThread;
        DelayTime.Value = DelayTimes;
        Unloaded += SettingsPage_Unloaded;
        SavaAppx.IsOn = global_cfg.cfg.JsonCfg.SaveAppx;
        AutoCheckUpdate.IsOn = global_cfg.cfg.JsonCfg.AutoCheckUpdate;
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
            DownThread = (int)DownloadThreads.Value;
            DelayTimes = (int)DelayTime.Value;
            global_cfg.cfg.JsonCfg.SaveAppx = SavaAppx.IsOn;
            global_cfg.cfg.JsonCfg.DownThread = DownThread;
            global_cfg.cfg.JsonCfg.DelayTimes = DelayTimes;
            global_cfg.cfg.JsonCfg.AutoCheckUpdate = AutoCheckUpdate.IsOn;
            global_cfg.cfg.SaveConfig();
        }
        catch (System.Exception exception)
        {
            Console.WriteLine(exception);
            throw;
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

    private void BackgroundModel_Base_OnClick(object sender, RoutedEventArgs e)
    {
        var until = ((RadioButton)sender);
        var tag = until.Tag.ToString();

        BackgroundEnum type = tag switch
        {
            "None" => BackgroundEnum.None,
            "Mica" => BackgroundEnum.Mica,
            "BaseAlt" => BackgroundEnum.BaseAlt,
            "Acrylic" => BackgroundEnum.Acrylic,
            "Color" => BackgroundEnum.Color,
            "Image" => BackgroundEnum.Image,
            _ => BackgroundEnum.None,
        };

        global_cfg.cfg.JsonCfg.BackgroundEnum = type;
        global_cfg.cfg.SaveConfig();

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
        {
            global_cfg.MainWindow.UpdateBackground();
        }));
    }
}
