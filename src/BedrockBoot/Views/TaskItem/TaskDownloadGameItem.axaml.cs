/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Entity.Progress;
using BedrockBoot.Standard.Entity.Task;
using BedrockBoot.Standard.Interface.Platform.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadGameItem : UserControl, ITaskItem
{
    private IDownload _downloader;
    private CancellationTokenSource _cancellationTokenSource;
    private string _taskStage = "";
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

    public TaskDownloadGameItem()
    {
        InitializeComponent();
    }

    public TaskDownloadGameItem(BuildInfo info, string url, bool? isUsePack, string dir, string gameName) : this()
    {
        BuildInfo = info;
        InstallFolder = dir;
        GameName = gameName;
        Url = url;
        IsUsePack = (bool)isUsePack!;

        _taskTitle = string.Format(I18nManager.Instance["Task.Game.Title.Format"], GameName, info.ID);
        InitializeDownloader();
    }

    public string InstallFolder { get; set; }
    public string GameName { get; set; }
    public string Url { get; set; }
    public bool IsUsePack { get; set; }
    public BuildInfo BuildInfo { get; set; }

    /// <summary>
    ///     本次安装是否真正成功。下载器的 Completed 回调只代表流程走到完成分支
    ///     （GDK 在解压完成时就触发），因此还要排除已经报过错的场景，
    ///     以及「回调已触发、但注册阶段被用户取消」的场景。
    ///     InstallAsync 内部会吞掉异常，调用方拿到正常返回并不代表安装成功。
    /// </summary>
    public bool IsInstallSucceeded => _installCompleted && !_installFailed && !_installCanceled;

    /// <summary>
    ///     本次安装是否被用户取消。取消后即使 Completed 已经触发过，也不能算成功：
    ///     GDK 的解压完成会先触发 Completed，随后的注册（Registered）阶段才是真正的终点。
    /// </summary>
    public bool IsInstallCanceled => _installCanceled;

    private bool _installCompleted;
    private bool _installFailed;
    private bool _installCanceled;

    private void InitializeDownloader()
    {
        _downloader = PlatformCore.CreateDownloader(BuildInfo, IsUsePack, InstallFolder, GameName);
        _downloader.DownloadProgress = (text, progressInfo) =>
            Dispatcher.UIThread.Invoke(() => UpdateDownloadProgress(text, progressInfo));

        _downloader.MergeProgress = (text, percentage) =>
            Dispatcher.UIThread.Invoke(() => UpdateMergeProgress(text, percentage));

        _downloader.ExtractionProgress = (text, percentage) =>
            Dispatcher.UIThread.Invoke(() => UpdateExtractionProgress(text, percentage));

        _downloader.DeploymentProgress = (text, progress) =>
            Dispatcher.UIThread.Invoke(() => UpdateDeploymentProgress(text, progress));

        _downloader.StatusText = text =>
            Dispatcher.UIThread.Invoke(() => MainText.Text = text);

        _downloader.InstallStateChanged = states =>
            Dispatcher.UIThread.Invoke(() => HandleInstallState(states));

        _downloader.ErrorOccurred = (title, message, ex) =>
            Dispatcher.UIThread.Invoke(() =>
            {
                _installFailed = true;
                ShowErrorDialog(title, message, ex);
            });

        _downloader.Completed = c =>
            Dispatcher.UIThread.Invoke(() =>
            {
                // Completed 在 GDK（Extracted）与非 GDK（Registered）分支都可能触发，
                // 且 InstallAsync 结束前还会再触发一次，这里只做标记，重复触发无副作用
                _installCompleted = true;

                // 取消之后才到的完成回调（GDK 解压完成即 Completed，注册阶段才被取消）
                // 不能把界面刷成"已完成"——真正结束与否由 IsInstallSucceeded 判定
                if (_installCanceled) return;

                MainSpeedText.Text = I18nManager.Instance["Task.Game.Status.Completed"];
                ReportProgress(100, I18nManager.Instance["Task.Game.Status.Completed"]);
            });
    }

    private void UpdateMergeProgress(string text, double percentage)
    {
        InsMergeBar.IsIndeterminate = false;
        InsMergeBar.Value = percentage;
        MainText.Text = text;
        ReportProgress(35 + percentage * 0.15, text);
    }

    public void Install(Action installed)
    {
        CardTitle.Text = _taskTitle;

        InsGetUrlBar.IsIndeterminate = true;
        if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
            InsInstallGamePanel.IsVisible = false;

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        Task.Run(async () =>
        {
            try
            {
                await _downloader.InstallAsync(Url, token);
            }
            catch (OperationCanceledException)
            {
                // 下载器内部一般已经把取消吞掉了，这里只是兜底
                _installCanceled = true;
            }
            finally
            {
                // 取消状态以令牌为准，而不是以 Completed 为准：
                // GDK 在解压完成（Extracted）时就触发 Completed，之后还有注册阶段，
                // 这段时间里取消，InstallAsync 会捕获取消并正常返回，
                // 只有这里才能把"已取消"和"已完成"区分开
                if (token.IsCancellationRequested) _installCanceled = true;
            }

            // 无论成功、失败还是取消都要回调，由调用方按 IsInstallSucceeded 决定后续动作
            installed?.Invoke();
        });
    }

    private void UpdateDownloadProgress(string text, DownloadProgressInfo progressInfo)
    {
        if (InsGetUrlBar.IsIndeterminate)
        {
            InsGetUrlBar.IsIndeterminate = false;
            InsGetUrlBar.Value = 100;
        }

        InsDownGameBar.Value = progressInfo.Percentage;
        MainText.Text = text;
        ReportProgress(progressInfo.Percentage * 0.35, text);

        if (!string.IsNullOrEmpty(progressInfo.Speed))
            MainSpeedText.Text = string.Format(I18nManager.Instance["Common.Unit.Speed"], progressInfo.Speed);
    }

    private void UpdateExtractionProgress(string text, double percentage)
    {
        InsUnZipBar.IsIndeterminate = false;
        InsUnZipBar.Value = percentage;
        MainText.Text = text;
        ReportProgress(50 + percentage * 0.25, text);
    }

    private void UpdateDeploymentProgress(string text, DeploymentProgressInfo progress)
    {
        InsInstallGameBar.Value = progress.Percentage;
        MainText.Text = text;
        ReportProgress(75 + progress.Percentage * 0.25, text);
    }

    private void HandleInstallState(InstallStates states)
    {
        switch (states)
        {
            case InstallStates.Extracting:
                InsUnZipBar.IsIndeterminate = false;
                break;

            case InstallStates.Extracted:
                InsUnZipBar.Value = 100;
                MainSpeedText.Text = I18nManager.Instance["Task.Game.Status.LocalInstalling"];
                ReportProgress(75, I18nManager.Instance["Task.Game.Status.LocalInstalling"]);
                break;

#if WINDOWS
            case InstallStates.Cleared:
                InsInstallGameBar.IsIndeterminate = true;
                break;

            case InstallStates.Registering:
                InsInstallGameBar.IsIndeterminate = false;
                break;

            case InstallStates.Registered:
                InsInstallGameBar.Value = 100;
                ReportProgress(100, I18nManager.Instance["Task.Game.Status.Completed"]);
                break;
#endif
        }
    }

    private void ShowErrorDialog(string title, string message, Exception ex)
    {
        if (ex != null)
            message += $"\n\n{string.Format(I18nManager.Instance["Task.Game.Error.Detail"], ex.Message)}";

        DialogHost.Show(new DialogInfo
        {
            Title = title,
            Content = message,
            CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"],
            AccountButton = DialogButtons.CloseButton
        });
    }

    public static void Install(
        BuildInfo info,
        string url,
        bool? isUsePack,
        string dir,
        string gameName,
        Action? installedCallBack = null)
    {
        // 下载开始提示：任务入队后立刻给一次反馈
        ShowNotice(
            string.Format(I18nManager.Instance["Task.Game.Notice.Added"], gameName),
            NoticeType.Info);

        var body = new TaskDownloadGameItem(info, url, isUsePack, dir, gameName);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => Dispatcher.UIThread.Post(() =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);

            if (body.IsInstallSucceeded)
            {
                // 安装完成提示
                ShowNotice(
                    string.Format(I18nManager.Instance["Task.Game.Notice.Success"], gameName),
                    NoticeType.Success);

                // 新版本落盘后立即刷新版本列表，避免要切页才看得到
                RefreshVersionLists();
            }
            else if (body.IsInstallCanceled)
            {
                // 用户取消：既不是成功也不是失败，别再刷"安装成功"，也不刷新列表
                ShowNotice(
                    string.Format(I18nManager.Instance["Task.Game.Notice.Cancelled"], gameName),
                    NoticeType.Info);
            }

            installedCallBack?.Invoke();
        }));
    }

    private static void ShowNotice(string message, NoticeType type)
    {
        GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.Game.Notice.Title"],
            Message = message,
            NoticeType = type
        });
    }

    private static void RefreshVersionLists()
    {
        // 实例管理页正在显示时由它自己重建列表（其 UpdateGameList 内部会顺带刷新主页版本下拉）；
        // 页面未显示则刷新主页版本下拉。逻辑集中在 MainManager，删除实例等场景共用。
        MainManager.RefreshVersionLists();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // 取消是"请求"不是"结果"：真正停下来要等 InstallAsync 抛令牌，可能还有一小段延迟，
        // 所以点下去先立刻给个反馈，并把按钮禁掉避免重复点
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested) return;

        if (sender is Button button) button.IsEnabled = false;

        var cancelling = I18nManager.Instance["Task.Game.Status.Cancelling"];
        MainText.Text = cancelling;
        ReportProgress(_taskProgress, cancelling, _taskIsIndeterminate);

        _cancellationTokenSource.Cancel();
    }
}