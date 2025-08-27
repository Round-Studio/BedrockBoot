using BedrockBoot.Controls.ContentDialogContent;
using BedrockBoot.Pages.ManagerPages;
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
    /// </summary>
    public sealed partial class ManagerPackPage : Page
    {
        public ManagerPackPage()
        {
            InitializeComponent();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (NavigationViewItem)args.SelectedItem;
            if ((string)selectedItem.Tag == "ResourcePack") NavFrame.Navigate(typeof(ManagerResourcePacksPage));
            if ((string)selectedItem.Tag == "World") NavFrame.Navigate(typeof(ManagerWorldsPage));
        }

        private async void ImportPack_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Content = new ImportMCPackContent(),
                Title = "导入资源",
                PrimaryButtonText = "导入",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };
            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                ((ImportMCPackContent)dialog.Content).StartImport();
            }
            NavFrame.Navigate(typeof(ManagerResourcePacksPage));
        }
    }
}
