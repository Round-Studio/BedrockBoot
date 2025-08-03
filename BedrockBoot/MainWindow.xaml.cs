using BedrockBoot.Pages;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MinecraftBoot;
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
            ExtendsContentIntoTitleBar = true;

            /*
            * 不要启用此代码，除非你想使用 DevWinUI-JSON 的导航服务，但事实上我根本没写好这个服务。:)
            App.Current.NavService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                                  .ConfigureDefaultPage(typeof(HomePage));
            App.Current.NavService.ConfigureSettingsPage(typeof(SettingsPage));
            App.Current.NavService.ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);*/

        }

        private HomePage HomePage { get; set; } = new();
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                var tag = ((NavigationViewItem)((NavView.SelectedItem))).Tag.ToString();

                var page = tag switch
                {
                    "HomePage" => HomePage
                };
                NavFrame.Navigate(page.GetType());
            }
            catch { }
        }
    }
}
