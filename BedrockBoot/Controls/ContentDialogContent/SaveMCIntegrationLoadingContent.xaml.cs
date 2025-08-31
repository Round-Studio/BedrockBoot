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
using BedrockBoot.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveMCIntegrationLoadingContent : Page
    {
        public SaveMCIntegrationLoadingContent(IntegrationInfo info,string file)
        {
            InitializeComponent();

            var save = new SaveIntegration(info);
            var prog = new Progress<(double, string)>();
            prog.ProgressChanged += (sender, tuple) =>
            {
                if (ProgressBar.IsIndeterminate) ProgressBar.IsIndeterminate = false;

                ProgressBar.Value = tuple.Item1;
                ProgressTextBlock.Text = tuple.Item2;
            };
            Task.Run(async () =>
            {
                var path = await save.StartMakeAndGetPackPath(prog);
                File.Copy(path, file, true);
                DispatcherQueue.TryEnqueue(() =>
                {
                    ((ContentDialog)this.Parent).Hide();

                    _ = EasyContentDialog.CreateDialog(((ContentDialog)this.Parent).XamlRoot, "导出完成", "整合包已导出");
                });
            });
        }
    }
}
