using BedrockBoot.Versions;
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
using System.Text.Json;
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
    public sealed partial class ImportPackContent : Page
    {
        public string PackPath => Pack_Path_Box.Text;
        public string PackType => ((ComboBoxItem)PackType_Box.SelectedItem).Content.ToString();
        public string Version => versionsList[VersionsList.SelectedIndex];
        private List<string> versionsList = new();
        public ImportPackContent()
        {
            InitializeComponent();

            var path = global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex];
            globalTools.SearchVersionJson(path.Path, ref versionsList, 0, 2);
            // 收集数据
            foreach (var c in versionsList)
            {
                var fullPath = Path.GetFullPath(c);
                try
                {
                    var nowVersions = JsonSerializer.Deserialize<NowVersions>(File.ReadAllText(fullPath));
                    if (nowVersions != null && !string.IsNullOrEmpty(nowVersions.Type))
                    {
                        VersionsList.Items.Add(new ComboBoxItem() { Content = nowVersions.DisPlayName });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载版本 {fullPath} 失败: {ex.Message}");
                }
            }
            if(versionsList.Count > 0)
            {
                VersionsList.SelectedIndex = 0;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePicker(WindowNative.GetWindowHandle(App.MainWindow));
            picker.FileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "资源部", new List<string> { "*.mcpack"} },
                { "Mod 文件", new List<string> { "*.dll"} }
            };

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Pack_Path_Box.Text = file.Path;
            }
        }
    }
}
