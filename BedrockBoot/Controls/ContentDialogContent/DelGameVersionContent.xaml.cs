using CommunityToolkit.WinUI;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.ContentDialogContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DelGameVersionContent : Page
    {
        public DelGameVersionContent(string path)
        {
            InitializeComponent();
            StartDelAsync(path);
        }
        private async Task StartDelAsync(string path)
        {
            try
            {
                await Task.Run(async () =>
                {
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList();

                    // 更新UI
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        Del_ProgressBar.Maximum = files.Count;
                        Del_ProgressBar.Value = 0;
                    });

                    // 分批处理以避免频繁的UI更新
                    int processed = 0;
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            processed++;

                            // 每处理10个文件更新一次UI，减少UI线程压力
                            if (processed % 10 == 0 || processed == files.Count)
                            {
                                await DispatcherQueue.EnqueueAsync(() =>
                                {
                                    Del_ProgressBar.Value = processed; 
                                    Del_ProgressTextBlock.Text = $"{(processed * 100.0 / files.Count):0.00} %";
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"删除失败 {file}: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"操作失败: {ex.Message}");
            }
            finally
            {
                Directory.Delete(path, true);
                // 关闭对话框
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    ((ContentDialog)this.Parent)?.Hide();
                });
            }
        }
    }
}
