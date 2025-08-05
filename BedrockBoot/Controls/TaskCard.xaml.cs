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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockLauncher.Core;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Native;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls
{
    public sealed partial class TaskCard : UserControl,IDisposable
    {
        public CancellationTokenSource CancellationToken = new CancellationTokenSource();
        public  VersionInformation Version { get; set; }

        public Action<TaskCard> completeCallback;
        public TaskCard()
        {
            InitializeComponent();
        }

        public void DoInstallAsync(string Name,string Install_dir,string appx_path)
        {
            new Thread((() =>
            {
                var installCallback = new InstallCallback()
                {
                    CancellationToken = CancellationToken.Token,
                    downloadProgress = (new Progress<DownloadProgress>((progress =>
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                        {
                            Process.Value = progress.ProgressPercentage;
                        }));

                    }))),
                    install_states = (states =>
                    {
                        switch (states)
                        {
                            case InstallStates.getingDownloadUri:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "获取Uri中..."; }));
                                break;
                            case InstallStates.gotDownloadUri:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "已获取Uri"; }));
                                break;
                            case InstallStates.registered:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "已注册Minecraft"; }));
                                break;
                            case InstallStates.registering:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() =>
                                {
                                    Button.IsEnabled = false;
                                    Process_Text.Text = "注册Minecraft中...请耐心等待";
                                }));
                                break;
                            case InstallStates.unzipng:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "解压中..."; }));
                                break;
                            case InstallStates.unziped:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "解压成功"; }));
                                break;
                            case InstallStates.downloading:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "下载中..."; }));
                                break;
                            case InstallStates.downloaded:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "下载完成"; }));
                                break;
                        }
                    }),
                    registerProcess_percent = ((s, u) =>
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                        {
                            Process.Value = u;
                        }));
                    }),
                    result_callback = ((status, exception) =>
                    {
                        if (exception!=null)
                        {
                            MessageBox.ShowAsync(exception);
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                            {
                                global_cfg.tasksPool.Remove(this);
                            }));
                        
                        }
                        else if (status == AsyncStatus.Completed)
                        {
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                            {
                                global_cfg.tasksPool.Remove(this);
                            }));
                        }
                    }),
                    zipProgress = (new Progress<ZipProgress>((progress =>
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                        {
                            Process.Value = progress.Percentage;
                        }));
                    })))
                };

                try
                {
                    global_cfg.core.InstallVersion(Version,Install_dir,appx_path,installCallback);
                }
                catch (Exception e)
                {
                    MessageBox.ShowAsync(e);

                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() =>
                    {
                        global_cfg.tasksPool.Remove(this);
                    }));
                }

            })).Start();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancellationToken.Cancel();
            global_cfg.tasksPool.Remove(this);
        }

        public void Dispose()
        {
            CancellationToken.Dispose();
        }
    }
}
