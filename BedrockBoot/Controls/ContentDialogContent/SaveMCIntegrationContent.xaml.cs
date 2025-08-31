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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Integration.Classes.Save;
using BedrockBoot.Integration.Entry;
using DevWinUI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveMCIntegrationContent : Page
    {
        private XamlRoot xamlRoot;
        public SaveMCIntegrationContent(XamlRoot xamlroot)
        {
            InitializeComponent();
            xamlRoot = xamlroot;
        }

        public async Task OnSave(VersionOntologyInfo info)
        {
            var picker = new SavePicker(WindowNative.GetWindowHandle(App.MainWindow));
            picker.FileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "Bedrock 整合包格式", new List<string> { "*.mcintegation" } }
            };
            picker.ShowAllFilesOption = false;
            picker.SuggestedFileName = "我的整合包.mcintegation";

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var filePath = file.Path;
                if (!filePath.EndsWith(".mcintegation"))
                {
                    filePath = $"{filePath}.mcintegation";
                }

                var dialog = new ContentDialog()
                {
                    XamlRoot = xamlRoot,
                    Title = "导出整合包...",
                    Content = new SaveMCIntegrationLoadingContent(new IntegrationInfo()
                    {
                        Author = Pack_Writer.Text,
                        Version = Pack_Version.Text,
                        Name = Pack_Name.Text,
                        VersionOntologyInfo = info,
                        UseMods = (bool)Pack_Mods.IsChecked,
                        UseDMods = (bool)Pack_DMods.IsChecked,
                        UseWorlds = (bool)Pack_Worlds.IsChecked,
                        UseResPacks = (bool)Pack_ResPacks.IsChecked
                    }, filePath)
                };
                await dialog.ShowAsync();
            }
        }

        public static async Task OpenSave(XamlRoot xamlRoot, VersionOntologyInfo info)
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = xamlRoot,
                Title = "导出整合包",
                Content = new SaveMCIntegrationContent(xamlRoot),
                CloseButtonText = "取消",
                PrimaryButtonText = "导出",
                DefaultButton = ContentDialogButton.Primary
            };

            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await ((SaveMCIntegrationContent)dialog.Content).OnSave(info);
            }
        }
    }
}
