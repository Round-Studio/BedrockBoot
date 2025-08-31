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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Models.Classes.Launch;
using BedrockBoot.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LaunchGameContent : Page
    {
        public LaunchGameContent()
        {
            InitializeComponent();
        }

        public async Task Launch(NowVersions versionInfo)
        {
            QuickLaunchGame.LaunchGame(versionInfo, (s, pr) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (Laun_ProgressBar.IsIndeterminate)
                    {
                        Laun_ProgressBar.IsIndeterminate = false;
                    }
                    Laun_ProgressTextBlock.Text = s;
                    Laun_ProgressBar.Value = pr;

                    if (pr == 100)
                    {
                        if(this.Parent != null)
                        {
                            if (this.Parent.GetType() == typeof(ContentDialog))
                            {
                                ((ContentDialog)this.Parent).Hide();
                            }
                        }
                    }
                });
            });
        }

        public static async void LaunchGame(XamlRoot xamlRoot,NowVersions versionInfo)
        {
            var body = new LaunchGameContent();
            var dialog = new ContentDialog()
            {
                XamlRoot = xamlRoot,
                Title = $"启动游戏 {versionInfo.VersionName}",
                Content = body,
            };
            dialog.ShowAsync();
            Task.Run(() =>
            {
                Thread.Sleep(1000);

                try
                {
                    body.Launch(versionInfo);
                }
                catch(Exception ex)
                {
                    EasyContentDialog.CreateDialog(xamlRoot, "错误", ex.ToString());
                }
            });
        }
    }
}
