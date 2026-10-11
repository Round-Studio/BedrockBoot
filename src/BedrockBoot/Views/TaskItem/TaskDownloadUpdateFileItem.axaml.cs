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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Standard.Entity.Progress;
using BedrockBoot.Standard.Entity.Task;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models;
using BedrockBoot.Models.Account.Github;
using BedrockBoot.Models.Global;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;
using Round.SDK.Entity;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;
using Path = System.IO.Path;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskDownloadUpdateFileItem : UserControl, ITaskItem
{
    private readonly string _currentExecutablePath = GetCurrentLauncherPath();

    private Action _cancelCallBack = () => { };
    private CancellationTokenSource? _cts;
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

    public TaskDownloadUpdateFileItem()
    {
        InitializeComponent();
    }

    public TaskDownloadUpdateFileItem(UpdateCheckResult result) : this()
    {
        Result = result;
        _taskTitle = string.Format(I18nManager.Instance["Task.Update.Title.Format"], result.DisplayName);
    }

    public UpdateCheckResult Result { get; set; } = null!;

    public void Update(Action cancelCallBack)
    {
        _cancelCallBack = cancelCallBack;
        CardTitle.Text = _taskTitle;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            string downloadUrl;
            string assetName;

            if (Result.Source == UpdateSource.Release)
            {
                var asset = SelectPreferredAsset();
                if (asset == null)
                {
                    Console.WriteLine(@"未找到适用于当前平台的更新资源");
                    return;
                }

                downloadUrl = asset.BrowserDownloadUrl;
                assetName = asset.Name;
            }
            else
            {
                var conf = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount);
                var artifact = await SelectPreferredArtifactAsync(conf.Data.UserToken);
                if (artifact == null)
                {
                    Console.WriteLine(@"未找到适用于当前平台的 Action 产物");
                    return;
                }

                downloadUrl = artifact.ArchiveDownloadUrl;
                assetName = $"{artifact.Name}.zip";
            }

            Directory.CreateDirectory(PathsList.UpdatePath);
            var downloadPath = Path.Combine(PathsList.UpdatePath, assetName);

            var download = new GithubFilesDownloader(
                GlobalModel.Config.Data.DownloadChunkCount,
                1024);

            await download.DownloadAsync(
                downloadUrl,
                downloadPath,
                new Progress<DownloadProgress>(progress =>
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
                }),
                token);

            await Task.Delay(100, token);

            if (string.IsNullOrWhiteSpace(_currentExecutablePath) || !File.Exists(_currentExecutablePath))
                throw new FileNotFoundException("无法定位当前程序，不能启动更新引导流程", _currentExecutablePath);

            if (Result.Source == UpdateSource.Action)
            {
                var extractDir = Path.Combine(PathsList.UpdatePath, "extracted");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, extractDir);

                var exe = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories)
                    .FirstOrDefault(f =>
                        OperatingSystem.IsWindows()
                            ? f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                            : f.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase));

                if (exe == null)
                    throw new FileNotFoundException("解压后未找到可执行文件", extractDir);

                downloadPath = exe;
            }

            AppUpdater.EnsureExecutableForCurrentPlatform(downloadPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = _currentExecutablePath,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-updatev2");
            startInfo.ArgumentList.Add(downloadPath);

            Process.Start(startInfo);

            await Task.Delay(100, token);
            Environment.Exit(0);
        }, token);
    }

    public static void Update(UpdateCheckResult result)
    {
        Models.Global.GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = I18nManager.Instance["Task.Update.Notice.Title"],
            Message = I18nManager.Instance["Task.Update.Notice.Message"],
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadUpdateFileItem(result);
        var taskId = Models.Global.GlobalModel.TaskManager.AddTask(body);

        body.Update(() => Models.Global.GlobalModel.TaskManager.RemoveTask(taskId));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _cancelCallBack.Invoke();
    }

    private ReleaseAsset? SelectPreferredAsset()
    {
        var assets = Result.Release?.Assets.ToList() ?? new();
        if (assets.Count == 0)
            return null;

        if (OperatingSystem.IsWindows())
            return assets.FirstOrDefault(asset =>
                       asset.Name.Contains("win", StringComparison.OrdinalIgnoreCase) &&
                       asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                   ?? assets.FirstOrDefault(asset =>
                       asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        if (OperatingSystem.IsLinux())
            return assets.FirstOrDefault(asset =>
                       asset.Name.Contains("linux", StringComparison.OrdinalIgnoreCase) &&
                       asset.Name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                   ?? assets.FirstOrDefault(asset =>
                       asset.Name.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase));

        return null;
    }

    private async Task<Artifact?> SelectPreferredArtifactAsync(string? userToken = null)
    {
        var run = Result.WorkflowRun;
        if (run == null)
            return null;

        var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

        if (!string.IsNullOrEmpty(userToken))
            github.Credentials = new Credentials(userToken);

        var artifacts = await github.Actions.Artifacts.ListWorkflowArtifacts(
            "Round-Studio", "BedrockBoot", run.Id);

        var list = artifacts.Artifacts.ToList();
        if (list.Count == 0)
            return null;

        if (OperatingSystem.IsWindows())
            return list.FirstOrDefault(a =>
                       a.Name.Contains("win", StringComparison.OrdinalIgnoreCase))
                   ?? list.FirstOrDefault(a =>
                       a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase))
                   ?? list.FirstOrDefault();

        if (OperatingSystem.IsLinux())
            return list.FirstOrDefault(a =>
                       a.Name.Contains("linux", StringComparison.OrdinalIgnoreCase))
                   ?? list.FirstOrDefault();

        return list.FirstOrDefault();
    }

    private static string GetCurrentLauncherPath()
    {
        if (OperatingSystem.IsLinux())
        {
            var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (!string.IsNullOrWhiteSpace(appImagePath))
                return appImagePath;
        }

        return Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    }
}