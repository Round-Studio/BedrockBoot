using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Models.Classes.Style.Background;
using BedrockBoot.Models.Enum.Background;
using BedrockBoot.Pages;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Models.Classes.Update;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            ExtendsContentIntoTitleBar = true;
            AppTitleBar.IsBackButtonVisible = false; 
            SetTitleBar(AppTitleBar);
            this.Closed += MainWindow_Closed;

            AppTitleBar.Title = $"BedrockBoot";
            AppTitleBar.Subtitle = $"v{Config.cfg_Version}";

            var manager = WinUIEx.WindowManager.Get(this);
            manager.MinHeight = 720;
            manager.MinWidth = 1200;
            manager.Width = 1200;
            manager.Height = 700;

            HomePage.OnSettingsPageAction+= () =>
            {
                NavView.SelectedItem = NavView.SettingsItem;
                NavFrame.Navigate(typeof(SettingsPage));
                
            };
            HomePage.OnVerionPageAction+= () =>
            {
                NavView.SelectedItem = NavView.MenuItems[2];
                NavFrame.Navigate(typeof(DownloadPage));
            };
            HomePage.OnStartAction+= () =>
            {
                NavView.SelectedItem = NavView.MenuItems[1];
                NavFrame.Navigate(typeof(VersionPage));
            };
            global_cfg.MainWindow = this;
            UpdateBackground();

            if(global_cfg.cfg.JsonCfg.AutoCheckUpdate) OnUpdate();
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            global_cfg.cfg.SaveConfig();
            Environment.Exit(0);
        }
        public async void UpdateBackground()
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
            {
                global_cfg.MainWindow.SystemBackdrop = new TransparentBackdrop();
                switch (global_cfg.cfg.JsonCfg.BackgroundEnum)
                {
                    case BackgroundEnum.None:
                        this.SystemBackdrop = null;
                        break;
                    case BackgroundEnum.Mica:
                        this.SystemBackdrop = new DevWinUI.MicaSystemBackdrop();
                        break;
                    case BackgroundEnum.BaseAlt:
                        this.SystemBackdrop = new DevWinUI.MicaSystemBackdrop(MicaKind.BaseAlt);
                        break;
                    case BackgroundEnum.Acrylic:
                        this.SystemBackdrop = new DevWinUI.AcrylicSystemBackdrop();
                        break;
                }
            }));
        }

        public async Task OnUpdate()
        {
            var update = new Update()
            {
                OnUpdate = (async (s1, s2,url) =>
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "��ǰ�и��¿���",
                        Content =
                            $"��ǰ��{s1.Replace("0", "").Replace(".", "")}\n���£�{s2.Replace("0", "").Replace(".", "").Replace("v", "")}",
                        CloseButtonText = "�ݲ�����",
                        PrimaryButtonText = "��������",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.Content.XamlRoot
                    };
                    var res = await dialog.ShowAsync();
                    if (res == ContentDialogResult.Primary)
                    {
                        var dialog_dow = new ContentDialog()
                        {
                            Title = "���ظ�����...",
                            Content = new DownloadUpdateFileContent(url),
                            XamlRoot = this.Content.XamlRoot
                        };
                        ((DownloadUpdateFileContent)dialog_dow.Content).StartDownload();
                        await dialog_dow.ShowAsync();
                    }
                })
            };
            await update.TryCheckUdate();
        }
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected) NavFrame.Navigate(typeof(SettingsPage));
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            global_cfg.cfg.SaveConfig();
            if ((string)selectedItem.Tag == "SettingPage") NavFrame.Navigate(typeof(SettingsPage));
            if ((string)selectedItem.Tag == "DownloadPage") NavFrame.Navigate(typeof(DownloadPage));
            if ((string)selectedItem.Tag == "HomePage") NavFrame.Navigate(typeof(HomePage));
            if ((string)selectedItem.Tag == "OOBE") NavFrame.Navigate(typeof(OOBEPage));
            if ((string)selectedItem.Tag == "TaskPage") NavFrame.Navigate(typeof(TaskPage));
            if ((string)selectedItem.Tag == "VersionPage") NavFrame.Navigate(typeof(VersionPage));
        }
    }
}
