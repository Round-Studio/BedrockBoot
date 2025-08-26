using BedrockBoot.Controls.FlyoutContent;
using BedrockBoot.Models.Classes.Helper;
using BedrockBoot.Models.Classes.Helper.Pack;
using BedrockBoot.Models.Entry.Pack;
using BedrockBoot.Tools;
using BedrockLauncher.Core.JsonHandle;
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
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages.ManagerPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManagerResourcePacksPage : Page
    {
        public ObservableCollection<ResourcePackManifestEntry> PackItems { get; set; } = new();
        public ManagerResourcePacksPage()
        {
            InitializeComponent();

            ResourcePackReader.ReadAnyResourcePacks().ForEach(x => { PackItems.Add(x); });
        }

        private void DeleteBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var entry = (ResourcePackManifestEntry)((Button)sender).Tag;
            if (Directory.Exists(entry.Path))
            {
                Directory.Delete(entry.Path, true);
                PackItems.Remove(entry);

                EasyContentDialog.CreateDialog(this.XamlRoot, "删除成功", $"资源包 {entry.Header.Name} 已删除");
            }
        }

        private async void SavePackBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var entry = (ResourcePackManifestEntry)((Button)sender).Tag;

            var picker = new SavePicker(WindowNative.GetWindowHandle(App.MainWindow));
            picker.FileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "MC 基岩版资源包", new List<string> { "*.mcpack" } },
            };
            picker.DefaultFileExtension = "*.mcpack";
            picker.SuggestedFileName = Path.GetFileName(entry.Path);
            picker.ShowAllFilesOption = false;

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var filePath = file.Path;
                if (!file.Path.EndsWith(".mcpack"))
                {
                    filePath += ".mcpack";
                }
                ZipHelper.CreateZipFromDirectory(entry.Path, filePath);
            }
        }
    }
}
