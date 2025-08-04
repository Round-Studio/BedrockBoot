using BedrockBoot.Pages.DownloadPages;
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

namespace BedrockBoot.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// 来自Dimeの馈赠
    /// </summary>
    public sealed partial class DownloadPage : Page
    {
        public DownloadPage()
        {
            global_cfg._downloadPage = this;
            InitializeComponent();

            SubPageFrame.Navigate(typeof(VersionsShowPages));
            BreadcrumbBar.ItemsSource = new string[] { "下载 Minecraft 实例" };
        }

        public void Navigate(Page page,string title)
        {
            BreadcrumbBar.ItemsSource = new string[] { "下载 Minecraft 实例", title };
            SubPageFrame.Navigate(page.GetType());
        }

        private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if(args.Index == 0)
            {
                SubPageFrame.Navigate(typeof(VersionsShowPages));
                BreadcrumbBar.ItemsSource = new string[] { "下载 Minecraft 实例" };
            }
        }
    }
}
