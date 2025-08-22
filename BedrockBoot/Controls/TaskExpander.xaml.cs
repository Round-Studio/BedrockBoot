using BedrockBoot.Models.Classes;
using BedrockBoot.Versions;
using BedrockLauncher.Core;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Native;
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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls
{
    public sealed partial class TaskExpander : UserControl,IDisposable
    {

        public bool IsCloseButtonVisible { get; set; } = true;

        private Visibility _IsCloseButtonVisible { get; set; } = Visibility.Visible;
        public string Header {get; set; } = "TaskExpander";
        public string HeaderIcon { get; set; } = "\uE821";
        public CancellationTokenSource CancellationToken = new CancellationTokenSource();
        public  VersionInformation Version { get; set; }
        public UIElement HeaderItems { get; set; }
        public UIElement Items { get; set; }
        public UIElement FooterItems { get; set; }
        private IconElement _HeaderIcon { get; set; } = new FontIcon() { Glyph = "\uF63C" };
        public Action<TaskExpander> completeCallback;
        public NowVersions nowVersions;
        private bool isError = false;
        public TaskExpander()
        {
            InitializeComponent();
            if (IsCloseButtonVisible)
            {
                _IsCloseButtonVisible = Visibility.Visible;
            }
            else
            {
                _IsCloseButtonVisible = Visibility.Collapsed;
            }

            try
            {
                _HeaderIcon = new FontIcon() { Glyph = HeaderIcon };
            }
            catch (Exception e)
            {
                // Debug.WriteLine($"Error setting HeaderIcon: {e.Message}");
                // TODO: 这里应该有一个日志记录
                // 6666666666666666666666666666
                _HeaderIcon = new FontIcon() { Glyph = "\uE821" }; // Fallback icon
            }

        }

        public void DoInstallAsync(string Name,string Install_dir,string appx_path,string backColor,string backImg,bool useAppx=false)
        {
            new Thread((() =>
            {
                nowVersions = new NowVersions()
                {
                    Version_Path = Install_dir,
                    VersionName = Name,
                    BackColor = backColor,
                    ImgBack = backImg,
                    Type = Version.Type,
                    RealVersion = Version.ID
                };

                var speedCalculator = new DownloadSpeedCalculator();
                var installCallback = new InstallCallback()
                {
                    CancellationToken = CancellationToken.Token,
                    downloadProgress = (new Progress<DownloadProgress>((progress =>
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                        {
                            Process.Value = progress.ProgressPercentage;
                            Process_JD_Text.Text = $"{progress.ProgressPercentage:0.00} %";
                            Process_File_Text.Text = $"{SpeedCalculatorExtensions.ToFileSizeString(progress.DownloadedBytes)} / {SpeedCalculatorExtensions.ToFileSizeString(progress.TotalBytes)}";

                            double instantSpeed = speedCalculator.CalculateSpeed(progress.DownloadedBytes, progress.TotalBytes);
                            Process_Speed_Text.Text = $"{instantSpeed:F2} MB/s";
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
                                var mods_dir = Path.Combine(Install_dir, "mods");
                                string delay_mods_dir = Path.Combine(Install_dir, "d_mods");
                                if (!Directory.Exists(mods_dir))
                                {
                                    Directory.CreateDirectory(mods_dir);
                                }
                                if (!Directory.Exists(delay_mods_dir))
                                {
                                    Directory.CreateDirectory(delay_mods_dir);
                                }
                                string url = "https://gitcode.com/gcw_lJgzYtGB/CONCRT140_APP/releases/download/v1.0.2/CONCRT140_APP.dll";
                                string targetPath = Path.Combine(Install_dir, "CONCRT140_APP.dll");
                                using (var http = new System.Net.Http.HttpClient())
                                {
                                    var data =  http.GetByteArrayAsync(url).Result;
                                    File.WriteAllBytes(targetPath, data);
                                }
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "已注册Minecraft"; }));
                                break;
                            case InstallStates.registering:
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() =>
                                {
                                    Button.IsEnabled = false;
                                    Process_Text.Text = "注册Minecraft中...";
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
                                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() => { Process_Text.Text = "下载完成，正在等待解压..."; }));
                                break;
                        }
                    }),
                    registerProcess_percent = ((s, u) =>
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                        {
                            Process.Value = u;
                            Process_JD_Text.Text = $"{u:0.00} %";
                            Process_File_Text.Text = s;
                            Process_Speed_Text.Text = "0 B/s";
                        }));
                    }),
                    result_callback = ((status, exception) =>
                    {
                        if (exception!=null)
                        {
                            isError = true;
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, (() =>
                            {
                                MessageBox.ShowAsync(exception);
                                global_cfg.tasksPool.Remove(this);
                            }));
                        }
                        else if (status == AsyncStatus.Completed)
                        {
                            nowVersions.RealVersion = Version.ID;
                            nowVersions.Type = Version.Type;
                            nowVersions.hasRegeister = true;
                            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, (() =>
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
                            Process_JD_Text.Text = $"{progress.Percentage:0.00} %";
                            Process_File_Text.Text = $"{progress.CompletedFiles} / {progress.TotalFiles} Files";
                            Process_Speed_Text.Text = $"0 B/s";
                        }));
                    })))
                };
                try
                {
                    GameBackGroundEditer gameBackGroundEditer = null;
                    if (string.IsNullOrEmpty(backImg) | string.IsNullOrEmpty(backColor))
                    {
                         gameBackGroundEditer = new GameBackGroundEditer()
                        {
                            file = backImg,
                            color = backColor,
                            isOpen = true
                        };
                    }
                    else
                    {
                        var directoryName = Path.GetDirectoryName(Path.Combine(Install_dir, Path.GetFileName(backImg)));
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        File.Copy(backImg, Path.Combine(Install_dir, Path.GetFileName(backImg)),true);
                        gameBackGroundEditer = new GameBackGroundEditer()
                        {
                            file = Path.GetFileName(backImg),
                            color = backColor,
                            isOpen = true
                        };
                    }
                    global_cfg.core.RemoveGame(GetVersionTypeByString(Version.Type));
                   if (useAppx)
                   {
                       global_cfg.core.InstallVersionByappx(appx_path,nowVersions.VersionName,Install_dir,installCallback,gameBackGroundEditer);
                   }
                   else
                   {
                        global_cfg.core.InstallVersion(Version.Variations[0], GetVersionTypeByString(Version.Type), appx_path, nowVersions.VersionName,Install_dir, installCallback, gameBackGroundEditer);
                   }

                   if (isError)
                   {
                       return;
                   }
                    var s = Path.Combine(global_cfg.cfg.JsonCfg.appxDir,
                        global_cfg.cfg.JsonCfg.appxName.Replace("{0}", Version.ID));
                    var combine = Path.Combine(Install_dir, "version.json");
                    File.WriteAllText(combine,JsonSerializer.Serialize(nowVersions));
                    if (!global_cfg.cfg.JsonCfg.SaveAppx)
                    {
                        File.Delete(s);
                    }
                }
                catch (Exception e)
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, (() =>
                    {
                        MessageBox.ShowAsync(e.ToString(), "错误");
                        global_cfg.tasksPool.Remove(this);
                       
                    }));
                }
            })).Start();
        }
        private VersionType GetVersionTypeByString(string type)
        {
            var result = VersionType.Release;

            switch (type.ToLower())
            {
                case "preview":
                    result = VersionType.Preview;
                    break;
                case "release":
                    result = VersionType.Release;
                    break;
                case "beta":
                    result = VersionType.Beta;
                    break;
            }

            return result;
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
