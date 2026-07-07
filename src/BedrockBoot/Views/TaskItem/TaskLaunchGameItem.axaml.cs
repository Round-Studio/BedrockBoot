using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper.Notice;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Linux;
using BedrockBoot.Views.Windows.SubWindows;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskLaunchGameItem : UserControl, ITaskItem
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    public bool IsCancel;
    public Action LaunchCompleted;
    private Process MinecraftProcess;
    private double _taskProgress;
    private string _taskStatusText = "";
    private string _taskTitle = "";
    private bool _taskIsCompleted;
    private bool _taskIsIndeterminate = true;

    public double Progress => _taskProgress;
    public string StatusText => _taskStatusText;
    public string Title => _taskTitle;
    public bool IsCompleted => _taskIsCompleted;
    public bool IsIndeterminate => _taskIsIndeterminate;

    public event Action<ITaskItem>? ProgressUpdated;

    protected void ReportProgress(double progress, string statusText, bool isIndeterminate = false)
    {
        _taskProgress = progress;
        _taskStatusText = statusText;
        _taskIsIndeterminate = isIndeterminate;
        if (progress >= 100) _taskIsCompleted = true;
        ProgressUpdated?.Invoke(this);
    }

    public TaskLaunchGameItem()
    {
        InitializeComponent();
    }

    public TaskLaunchGameItem(VersionConfig info) : this()
    {
        VersionInfo = info;
        _cancellationTokenSource = new CancellationTokenSource();
        _taskTitle = string.Format(I18nManager.Instance["Task.Launch.Title.Format"], info.Info.VersionName);
    }

    public VersionConfig VersionInfo { get; set; }

    public void Launch(Action launchCompleted)
    {
        if (VersionInfo.Config.IsEditModel)
            EditModule.IsVisible = true;

        LaunchCompleted = launchCompleted;
        // 标题国际化
        CardTitle.Text = _taskTitle;

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
                    ReportProgress(percentage, string.Format(I18nManager.Instance["Task.Launch.Status.Progress"], percentage, status));
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LaunchProgressText.Text = string.Format(I18nManager.Instance["Task.Launch.Status.Progress"],
                            percentage, status);
                        LaunchProgressBar.Value = percentage;
                    });
                };
#endif

#if LINUX
                lc.NoRunTool = () =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Content = "当前您正在 Linux 环境下运行本启动器\n" +
                                  "我们需要 ProtonGDK 组件才能正常启动 Minecraft for Windows (GDK)\n" +
                                  "\n" +
                                  "现在我们需要您同意 ProtonGDK 组件的下载",
                        Title = "必要运行时下载",
                        CloseButtonText = "立即下载",
                        PrimaryButtonText = "退出启动器",
                        AccountButton = DialogButtons.CloseButton,
                        PrimaryAction = () =>
                        {
                            Console.WriteLine("用户不同意下载 ProtonGDK，正在退出启动器...");
                            Environment.Exit(0);
                        },
                        CloseAction = () =>
                        {
                            var dialog = new DialogDownloadProtonGDKContent();
                            DialogHost.Show(new DialogInfo()
                            {
                                Content = dialog,
                                Title = "下载游戏运行组件"
                            });
                            dialog.Download();
                        }
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