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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Progress;
using BedrockBoot.Standard.Entity.Task;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Entity;
using GlobalModel = BedrockBoot.Models.Global.GlobalModel;

namespace BedrockBoot.Views.TaskItem;

/// <summary>
///     PreLoad.NET 组件下载的触发场景，只用来区分失败提示的标题：
///     启动阶段失败要说明「启动失败」，后台/手动检查失败则属于「检查更新失败」。
/// </summary>
public enum PreLoadDownloadTrigger
{
    /// <summary>启动器启动时的后台静默检查、设置页手动检查、启动器更新前的组件检查</summary>
    CheckUpdate,

    /// <summary>启动游戏前的强制守卫：组件缺失会直接拦截启动</summary>
    Startup
}

/// <summary>
///     PreLoad.NET 组件更新的后台下载任务项。
///     下载完成后替换本地组件文件并写入版本记录，下次启动游戏生效。
/// </summary>
public partial class TaskDownloadPreLoadItem : UserControl, ITaskItem
{
    private readonly ReleaseAsset? _asset;
    private readonly string? _directUrl;
    private readonly string _tagName;
    private readonly PreLoadDownloadTrigger _trigger;

    /// <summary>PreLoad 下载/替换共用同一落盘路径与目标 DLL，重叠任务必须串行执行</summary>
    private static readonly SemaphoreSlim DownloadGate = new(1, 1);

    private Action _removeCallBack = () => { };
    private CancellationTokenSource? _cts;
    private TaskCompletionSource<bool>? _tcs;
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

    public TaskDownloadPreLoadItem()
    {
        InitializeComponent();
    }

    public TaskDownloadPreLoadItem(ReleaseAsset asset, string tagName,
        PreLoadDownloadTrigger trigger = PreLoadDownloadTrigger.CheckUpdate) : this()
    {
        _asset = asset;
        _tagName = tagName;
        _trigger = trigger;
        _taskTitle = I18nManager.Instance["Task.PreLoad.Update.Title"];
    }

    /// <summary>
    ///     直链构造：不依赖 ReleaseAsset，用于 API 不可用时的 latest 直链（GitHub / CNB）。
    ///     <paramref name="tagName" /> 已知时用它写版本记录，未知则只落地文件。
    /// </summary>
    public TaskDownloadPreLoadItem(string directUrl, string? tagName = null,
        PreLoadDownloadTrigger trigger = PreLoadDownloadTrigger.CheckUpdate) : this()
    {
        _directUrl = directUrl;
        _tagName = tagName ?? "";
        _trigger = trigger;
        _taskTitle = I18nManager.Instance["Task.PreLoad.Update.Title"];
    }

    public void Update(Action removeCallBack)
    {
        _removeCallBack = removeCallBack;
        CardTitle.Text = _taskTitle;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            try
            {
                var suffix = string.IsNullOrEmpty(_tagName) ? "latest" : _tagName;
                var downloadPath = Path.Combine(PathsList.TempPath, $"PreLoad.NET.{suffix}.dll.download");
                if (Directory.Exists(PathsList.TempPath) is false) Directory.CreateDirectory(PathsList.TempPath);

                var progressHandler = new Progress<DownloadProgress>(progress =>
                {
                    ReportProgress(progress.ProgressPercentage, $"{progress.ProgressPercentage:F2} %");
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ProgressBar.IsIndeterminate)
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Value = 100;
                        }

                        ProgressBar.Value = (int)progress.ProgressPercentage;
                        ProgressText.Text = $"{progress.ProgressPercentage:F2} %";
                    });
                });

                // 下载与替换共用同一落盘路径和目标 DLL：两个任务重叠时，
                // 后到的任务可能截断前者的文件、或旧版本的慢任务把新 DLL 覆盖回去，必须串行
                await DownloadGate.WaitAsync(token);
                try
                {
                    if (_directUrl != null)
                    {
                        // 直链是普通网页地址，不需要也不应该先取令牌——令牌刷新失败（网络抖动等）
                        // 不能阻断这条本可匿名完成的下载路径
                        if (IsGitHubUrl(_directUrl))
                        {
                            var directDownload = new GithubFilesDownloader(
                                BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount, 1024);
                            await directDownload.DownloadAsync(_directUrl, downloadPath, progressHandler, token);
                        }
                        else
                        {
                            using var directDownloader = new MultiThreadDownloader(
                                BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount, 1024);
                            await directDownloader.DownloadAsync(_directUrl, downloadPath, progressHandler, token);
                        }
                    }
                    else
                    {
                        // 私有仓库需要授权头；令牌过期且无法续期时退回免鉴权下载（公开仓库仍可用）
                        string? userToken = null;
                        try
                        {
                            userToken = await GitHubTokenManager.GetValidTokenAsync();
                        }
                        catch (GitHubReauthRequiredException ex)
                        {
                            Console.WriteLine($@"PreLoad.NET 下载未使用授权头：{ex.Message}");
                        }

                        if (string.IsNullOrEmpty(userToken))
                        {
                            // 未登录只能走免鉴权的浏览器直连（公开仓库可用）
                            var download = new GithubFilesDownloader(
                                BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount, 1024);
                            await download.DownloadAsync(_asset!.BrowserDownloadUrl, downloadPath, progressHandler, token);
                        }
                        else
                        {
                            // 私有仓库的资产必须带授权头直连 api.github.com，
                            // 加速代理不覆盖该接口，也不能走无鉴权的 GithubFilesDownloader
                            using var downloader = new MultiThreadDownloader(
                                BedrockBoot.Core.Global.GlobalModel.Config.Data.DownloadChunkCount, 1024);
                            downloader.AdditionalHeaders = new Dictionary<string, string>
                            {
                                { "Authorization", $"Bearer {userToken}" },
                                { "Accept", "application/octet-stream" }
                            };

                            await downloader.DownloadAsync(_asset!.Url, downloadPath, progressHandler, token);
                        }
                    }

                    await Task.Delay(100, token);

                    PreLoadDllStore.ReplaceDll(await File.ReadAllBytesAsync(downloadPath));
                    if (!string.IsNullOrEmpty(_tagName))
                        MultiplayerDependencyHelper.WriteLocalVersion(PathsList.PreLoadVersionFile,
                            "PreLoad.NET", _tagName);

                    try
                    {
                        if (File.Exists(downloadPath)) File.Delete(downloadPath);
                    }
                    catch
                    {
                    }

                    ReportProgress(100, I18nManager.Instance["Task.PreLoad.Update.Done"]);
                    _tcs?.TrySetResult(true);
                }
                finally
                {
                    DownloadGate.Release();
                }

                // 让用户看到完成状态再移除任务项
                await Task.Delay(1500, CancellationToken.None);
                _removeCallBack.Invoke();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Console.WriteLine(@"PreLoad.NET 组件下载任务已取消");
                _tcs?.TrySetResult(false);
            }
            catch (Exception ex)
            {
                // 同上：只改属性不改控件，界面上看不到失败
                Console.WriteLine($@"PreLoad.NET 组件下载失败: {ex}");
                ShowFailure(string.Format(I18nManager.Instance["Task.Update.Error.FailFormat"], ex.Message));
                _tcs?.TrySetResult(false);
            }
        }, token);
    }

    /// <summary>切到失败状态：停止进度动画并把错误文案写到卡片上</summary>
    private void ShowFailure(string message)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowFailure(message));
            return;
        }

        _taskProgress = 100;
        _taskStatusText = message;
        _taskIsCompleted = true;
        _taskIsIndeterminate = false;

        ProgressBar.IsIndeterminate = false;
        ProgressBar.IsVisible = false;
        ProgressText.Text = message;
        if (!ProgressText.Classes.Contains("critical"))
            ProgressText.Classes.Add("critical");

        ProgressUpdated?.Invoke(this);

        GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
        {
            // 启动游戏前的守卫失败要说"启动失败"，后台静默检查/设置页手动检查失败则是"检查更新失败"
            Title = I18nManager.Instance[_trigger == PreLoadDownloadTrigger.Startup
                ? "Task.PreLoad.Notice.StartupFailedTitle"
                : "Task.PreLoad.Notice.CheckFailedTitle"],
            Message = message,
            NoticeType = NoticeType.Error
        });
    }

    public static void StartDownload(ReleaseAsset asset, string tagName)
    {
        _ = StartDownloadAndWait(asset, tagName);
    }

    /// <summary>
    ///     派发下载任务并等待结果：true = 已下载并替换成功，false = 失败/取消。
    ///     启动守卫用它阻塞等待组件就绪。
    /// </summary>
    public static Task<bool> StartDownloadAndWait(ReleaseAsset asset, string tagName,
        PreLoadDownloadTrigger trigger = PreLoadDownloadTrigger.CheckUpdate) =>
        StartTaskAndWait(() => new TaskDownloadPreLoadItem(asset, tagName, trigger));

    /// <summary>
    ///     直链入口：API 不可用时按 latest 直链下载并等待结果。
    ///     <paramref name="tagName" /> 已知时顺带写版本记录。
    /// </summary>
    public static Task<bool> StartDirectDownloadAndWait(string directUrl, string? tagName = null,
        PreLoadDownloadTrigger trigger = PreLoadDownloadTrigger.CheckUpdate) =>
        StartTaskAndWait(() => new TaskDownloadPreLoadItem(directUrl, tagName, trigger));

    /// <summary>直链是否属于 GitHub（决定要不要套加速源）</summary>
    private static bool IsGitHubUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));

    private static Task<bool> StartTaskAndWait(Func<TaskDownloadPreLoadItem> factory)
    {
        // 启动时自动检查在后台线程调用本入口，UserControl 必须在 UI 线程构造，
        // 否则样式/动画关键帧归属后台线程，绑定回调时抛跨线程访问异常
        return Dispatcher.UIThread.Invoke(() =>
        {
            GlobalModel.MainWindow?.Notice.AddNotice(new NoticeInfo
            {
                Title = I18nManager.Instance["Task.PreLoad.Update.Title"],
                Message = I18nManager.Instance["Task.PreLoad.Update.Notice.Message"],
                NoticeType = NoticeType.Info
            });

            var body = factory();
            body._tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var taskId = GlobalModel.TaskManager.AddTask(body);

            body.Update(() => GlobalModel.TaskManager.RemoveTask(taskId));
            return body._tcs!.Task;
        });
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _removeCallBack.Invoke();
    }
}
