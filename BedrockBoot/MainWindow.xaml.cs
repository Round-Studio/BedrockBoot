using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Pages;
using DevWinUI;
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
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            MessageBox.ShowAsync("正在关闭", "正在关闭");
            Environment.Exit(0);
        }
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            /*if (NavFrame.CanGoBack)
            {
                BackButton.Visibility = Visibility.Visible;
            }
            else
            {
                BackButton.Visibility = Visibility.Collapsed;
            }*/
            //
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavFrame.CanGoBack)
            {
                NavFrame.GoBack();
            }
        }
    }
}
