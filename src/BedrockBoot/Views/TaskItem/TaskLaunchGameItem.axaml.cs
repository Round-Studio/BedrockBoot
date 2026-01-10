using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockBoot.Views.DialogContent;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using PeNet;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskLaunchGameItem : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public bool IsCancel = false;
    public Action LaunchCompleted;
    private Process MinecraftProcess;
    private CancellationTokenSource _cancellationTokenSource;

    public TaskLaunchGameItem()
    {
        InitializeComponent();
    }

    public TaskLaunchGameItem(VersionConfig info) : this()
    {
        VersionInfo = info;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Launch(Action launchCompleted)
    {
        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        LaunchCompleted = launchCompleted;
        CardTitle.Text = $"启动游戏 {VersionInfo.Info.VersionName}";
        Console.WriteLine(
            $"正在启动：{VersionInfo.Info.VersionName} ({VersionInfo.Info.Version}) Type：{VersionInfo.Info.VersionType} {VersionInfo.Info.BuildType}");

        Task.Run(async () =>
        {
            try
            {
                if (IsCancel) return;

                Dispatcher.UIThread.Invoke(() =>
                {
                    LaunchProgressBar.IsIndeterminate = false;
                    CancelBtn.IsEnabled = true;
                });


                var lc = new EasyLauncher(VersionInfo);

                // 设置迁移回调
                lc.OnMigration = () =>
                {
                    // 触发启动完成回调（如果需要关闭启动界面）
                    LaunchCompleted?.Invoke();

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Show(new DialogInfo()
                        {
                            Title = "迁移通知",
                            Content = "该版本需要迁移\n" +
                                      "请问是否立即启动迁移？",
                            CloseButtonText = "启动迁移",
                            PrimaryButtonText = "不了",
                            CloseAction = () =>
                            {
                                DialogHost.Show(new DialogInfo()
                                {
                                    Title = "迁移版本",
                                    Content = new DialogMigrationGameRootConfigContent(VersionInfo)
                                });
                            }
                        });
                    });
                };

                // 设置进度更新回调
                lc.UpdateProgress = (status, percentage) =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LaunchProgressText.Text = $"{status} ({percentage:F2}%)";
                        LaunchProgressBar.Value = percentage;
                    });
                };

                // 设置进度文本更新回调
                lc.UpdateProgressText = (text) =>
                {
                    Dispatcher.UIThread.Invoke(() => { LaunchProgressText.Text = text; });
                };

                // 设置进度条模式回调
                lc.SetProgressIndeterminate = (isIndeterminate) =>
                {
                    Dispatcher.UIThread.Invoke(() => { LaunchProgressBar.IsIndeterminate = isIndeterminate; });
                };

                // 设置启动完成回调
                lc.LaunchCompleted = () => { Dispatcher.UIThread.Invoke(() => { LaunchCompleted?.Invoke(); }); };

                // 设置游戏启动回调
                lc.Launched = (process) =>
                {
                    Console.WriteLine($"游戏已启动，进程ID: {process.Id}");
                    // 可以在这里执行游戏启动后的其他操作
                    MinecraftProcess = process;
                };

                // 启动游戏
                await lc.Launch();
            }
            catch (TaskCanceledException)
            {
                // 用户取消操作
                Console.WriteLine("启动任务被取消");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动失败: {ex.Message}");
                Dispatcher.UIThread.Post(() => LaunchCompleted?.Invoke());
            }
        });
    }

    private async Task WaitForProcessExitAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync(_cancellationTokenSource.Token);

            Console.WriteLine($"游戏进程 PID：{MinecraftProcess.Id} 已退出");
            // 进程正常退出
            Console.WriteLine($"进程已退出，退出代码: {process.ExitCode}");
            Dispatcher.UIThread.Post(() => LaunchCompleted?.Invoke());
        }
        catch (TaskCanceledException)
        {
            // 用户取消了等待
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        Console.WriteLine($"进程已退出 (事件触发)，退出代码: {MinecraftProcess?.ExitCode}");

        // 确保在UI线程调用回调
        Dispatcher.UIThread.Post(() => { LaunchCompleted?.Invoke(); });

        // 清理事件处理器
        if (MinecraftProcess != null)
        {
            MinecraftProcess.Exited -= OnProcessExited;
        }
    }

    public static void Launch(VersionConfig gameInfo)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo()
        {
            Title = "启动游戏",
            Message = $"游戏 {gameInfo.Info.VersionName} 已将其启动任务添加至任务列表。",
            NoticeType = NoticeType.Info
        });

        var body = new TaskLaunchGameItem(gameInfo);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Launch(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }

    private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        this.IsCancel = true;

        // 取消相关操作
        _cancellationTokenSource?.Cancel();

        if (MinecraftProcess != null && !MinecraftProcess.HasExited)
        {
            try
            {
                MinecraftProcess.Kill(true);
                Console.WriteLine($"游戏进程 PID：{MinecraftProcess.Id} 已退出");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"终止进程时出错: {ex.Message}");
            }
        }

        // 清理资源
        MinecraftProcess?.Dispose();
        MinecraftProcess = null;

        // 调用完成回调
        LaunchCompleted?.Invoke();
    }

    // 添加 Process 的 WaitForExitAsync 扩展方法
    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            process.EnableRaisingEvents = true;
            process.Exited += OnExited;

            if (process.HasExited)
            {
                tcs.TrySetResult(true);
            }

            cancellationToken.Register(() =>
            {
                if (!process.HasExited)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
            });

            return tcs.Task;

            void OnExited(object sender, EventArgs e)
            {
                process.Exited -= OnExited;
                tcs.TrySetResult(true);
            }
        }
    }
}