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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
            ExtendsContentIntoTitleBar = true; // 让标题栏扩展到内容区域
            AppTitleBar.IsBackButtonVisible = false; // 不显示返回按钮
            SetTitleBar(AppTitleBar); // 设置自定义标题栏

            /* 不要启用此代码，除非你想使用 DevWinUI-JSON 的导航服务，但事实上我根本没写好这个服务。:)
            App.Current.NavService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                                  .ConfigureDefaultPage(typeof(HomePage));
            App.Current.NavService.ConfigureSettingsPage(typeof(SettingsPage));
            App.Current.NavService.ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
                                  .ConfigureTitleBar(AppTitleBar)
                                  .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);

            byd 没写好还加上来 可以在这留言，我看能留言多长 😡😡😡
            */
        }
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // DM: 越老，越好用
            if (args.IsSettingsSelected) NavFrame.Navigate(typeof(SettingsPage));

            var selectedItem = (NavigationViewItem)args.SelectedItem;
            if ((string)selectedItem.Tag == "SettingPage") NavFrame.Navigate(typeof(SettingsPage));
            if ((string)selectedItem.Tag == "DownloadPage") NavFrame.Navigate(typeof(DownloadPage));
            if ((string)selectedItem.Tag == "HomePage") NavFrame.Navigate(typeof(HomePage));
            if ((string)selectedItem.Tag == "OOBE") NavFrame.Navigate(typeof(OOBEPage));
            if ((string)selectedItem.Tag == "TaskPage") NavFrame.Navigate(typeof(TaskPage));
        }
    }
}
