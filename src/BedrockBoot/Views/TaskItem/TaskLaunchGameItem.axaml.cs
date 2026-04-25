using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper.Notice;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Windows.SubWindows;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskLaunchGameItem : UserControl
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    public bool IsCancel;
    public Action LaunchCompleted;
    private Process MinecraftProcess;

    public TaskLaunchGameItem()
    {
        InitializeComponent();
    }

    public TaskLaunchGameItem(VersionConfig info) : this()
    {
        VersionInfo = info;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public VersionConfig VersionInfo { get; set; }

    public void Launch(Action launchCompleted)
    {
        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        LaunchCompleted = launchCompleted;
        // 标题国际化
        CardTitle.Text = string.Format(I18nManager.Instance["Task.Launch.Title.Format"], VersionInfo.Info.VersionName);

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

#if WINDOWS
                // 设置迁移回调 - 这里的对话框内容已全部国际化
                lc.OnMigration = () =>
                {
                    LaunchCompleted?.Invoke();

                    Dispatcher.UIThread.Invoke(() =>
                    {
                        DialogHost.Show(new DialogInfo
                        {
                            Title = I18nManager.Instance["Task.Launch.Migration.Title"],
                            Content = I18nManager.Instance["Task.Launch.Migration.Content"],
                            CloseButtonText = I18nManager.Instance["Task.Launch.Migration.Action.Start"],
                            PrimaryButtonText = I18nManager.Instance["Task.Launch.Migration.Action.Later"],
                            CloseAction = () =>
                            {
                                DialogHost.Show(new DialogInfo
                                {
                                    Title = I18nManager.Instance["Task.Launch.Migration.View.Title"],
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
                        // 统一进度条文本格式
                        LaunchProgressText.Text = string.Format(I18nManager.Instance["Task.Launch.Status.Progress"],
                            percentage, status);
                        LaunchProgressBar.Value = percentage;
                    });
                };
#endif

                lc.UpdateProgressText = text =>
                {
                    Dispatcher.UIThread.Invoke(() => { LaunchProgressText.Text = text; });
                };

                lc.SetProgressIndeterminate = isIndeterminate =>
                {
                    Dispatcher.UIThread.Invoke(() => { LaunchProgressBar.IsIndeterminate = isIndeterminate; });
                };

                lc.LaunchCompleted = () =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LaunchCompleted?.Invoke();
                        if (!GlobalModel.MainWindow.IsWindowActive)
                            NoticeHelper.SentNotice("游戏退出", $"游戏 {VersionInfo.Info.VersionName} 已退出。");
                    });
                };

                lc.Launched = process =>
                {
                    MinecraftProcess = process;
#if WINDOWS
                    Dispatcher.UIThread.Invoke(() => new OverlayWindow(process, VersionInfo.Info.Version).Show());
#endif
                };

                await lc.Launch();
            }
            catch (TaskCanceledException)
            {
                // 可选：添加取消状态的 UI 反馈
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => LaunchCompleted?.Invoke());
            }
        });
    }

    public static void Launch(VersionConfig gameInfo)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.Launch.Notice.Title"],
            Message = string.Format(I18nManager.Instance["Task.Launch.Notice.Added"], gameInfo.Info.VersionName),
            NoticeType = NoticeType.Info
        });

        var body = new TaskLaunchGameItem(GameInfoHelper.GetVersionConfig(gameInfo.VersionPath));
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Launch(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }

    private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        IsCancel = true;
        _cancellationTokenSource?.Cancel();

        if (MinecraftProcess != null && !MinecraftProcess.HasExited)
            try
            {
                MinecraftProcess.Kill(true);
            }
            catch
            {
                /* Ignore */
            }

        MinecraftProcess?.Dispose();
        MinecraftProcess = null;

        LaunchCompleted?.Invoke();
    }
}