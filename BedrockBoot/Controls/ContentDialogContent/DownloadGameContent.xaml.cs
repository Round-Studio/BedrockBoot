using CommunityToolkit.WinUI.Helpers;
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
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadGameContent : Page
    {
        public string Path => System.IO.Path.Combine(global_cfg.cfg.JsonCfg.GameFolders[ChooseDownloadFolderPathComboBox.SelectedIndex].Path, NameBox.Text);
        public string Name => NameBox.Text;
        public string BackColor => RgbToHex(colorPicker.SelectedColor.R,colorPicker.SelectedColor.G, colorPicker.SelectedColor.B);
        public string ImgBack => imgback.Text;
        public bool IsUseAppx => UseAppx.IsOn;

        public string SeleteDir =>
            global_cfg.cfg.JsonCfg.GameFolders[ChooseDownloadFolderPathComboBox.SelectedIndex].Path;
        public string APPX_dir => System.IO.Path.Combine(
            global_cfg.cfg.JsonCfg.GameFolders[ChooseDownloadFolderPathComboBox.SelectedIndex].Path, "Appx");
        public static string RgbToHex(byte red, byte green, byte blue)
        {
            return $"#{red:X2}{green:X2}{blue:X2}";
        }
        public DownloadGameContent(string name)
        {
            InitializeComponent();
            NameBox.Text = name;
            UseAppx.IsEnabled = false;
            global_cfg.cfg.JsonCfg.GameFolders.ForEach(folder =>
            {
                ChooseDownloadFolderPathComboBox.Items.Add(new ComboBoxItem()
                {
                    Content = $"{folder.Name} - {folder.Path}"
                });
                var s = System.IO.Path.Combine(System.IO.Path.Combine(folder.Path,"Appx"),global_cfg.cfg.JsonCfg.appxName.Replace("{0}", name));
                if (File.Exists(s))
                {
                    UseAppx.IsEnabled = true;
                }
            });
            ChooseDownloadFolderPathComboBox.SelectedIndex = global_cfg.cfg.JsonCfg.ChooseFolderIndex;
        }
        private async void ButtonBaseImg_OnClick(object sender, RoutedEventArgs e)
        {
            var picker = new FilePicker(WindowNative.GetWindowHandle(App.MainWindow));
            picker.FileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "Images", new List<string> { "*.png", "*.jpg", "*.jpeg", "*.bmp" } },
            };

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                imgback.Text = file.Path;
            }
        }

        
    }
}
