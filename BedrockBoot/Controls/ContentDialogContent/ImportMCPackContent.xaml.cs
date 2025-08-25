using BedrockBoot.Models.Classes.Helper;
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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImportMCPackContent : Page
    {
        public string Path;
        public ImportMCPackContent()
        {
            InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePicker(WindowNative.GetWindowHandle(App.MainWindow));
            picker.FileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "资源包", new List<string> { "*.mcpack"} },
                { "存档，地图", new List<string> { "*.mcworld"} },
            };


            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Pack_Path_Box.Text = file.Path;
                Path = file.Path;
            }
        }
        public void StartImport()
        {
            if (Path.EndsWith(".mcpack"))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                    "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                    "LocalState", "games", "com.mojang", "resource_packs");

                ZipFile.ExtractToDirectory(Path, minecraftPath, true);
            }
            else
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string minecraftPath = System.IO.Path.Combine(localAppData, "Packages",
                    "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                    "LocalState", "games", "com.mojang", "minecraftWorlds", $"{RandomStringGenerator.GenerateRandomString()}=");

                ZipFile.ExtractToDirectory(Path, minecraftPath, true);

                var dialog = new ContentDialog()
                {

                };
            }
        }
    }
}
