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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Core.Models.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;
using BedrockBoot.Standard.Core;
using BedrockBoot.Standard.Interface.Platform.Game;
using BedrockBoot.Models;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DialogContent;

namespace BedrockBoot.Views.Windows.SystemMethod;

public partial class LaunchGameWindow : Window
{
    private readonly VersionConfig? _versionInfo;
    private bool _isClosing;
    private DateTime _lastUpdateTime;
    private readonly TimeSpan _updateThrottle = TimeSpan.FromMilliseconds(100);
    private Process? _gameProcess;
    private bool _gameLaunched;
    private TaskCompletionSource<bool>? _uwpChoiceTcs;
    private List<(string Name, string Version)>? _uwpMissingDeps;

    public LaunchGameWindow()
    {
        InitializeComponent();
        
        try
        {
            if (Program.Args == null || !Program.Args.Contains("-jump"))
                throw new Exception(I18nManager.Instance["LaunchWindow.Error.InvalidArgs"]);

            var index = Program.Args.FindIndex(a => a == "-jump");
            if (index + 1 >= Program.Args.Count)
                throw new Exception(I18nManager.Instance["LaunchWindow.Error.MissingPath"]);

            var folder = Program.Args[index + 1];
            if (!Directory.Exists(folder))
                throw new Exception(I18nManager.Instance["LaunchWindow.Error.PathNotFound"]);

            _versionInfo = GameInfoHelper.GetVersionConfig(folder);
            
            if (_versionInfo?.Info?.VersionName != null)
            {
                GameNameBox.Text = string.Format(I18nManager.Instance["LaunchWindow.Status.Launching"], _versionInfo.Info.VersionName);
            }
        }
        catch (Exception ex)
        {
            ProgressBox.Text = string.Format(I18nManager.Instance["LaunchWindow.Error.Format"], ex.Message);
            // 延迟关闭
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                Dispatcher.UIThread.Post(Close);
            });
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // 启动游戏
        _ = LaunchAsync();
    }

    private void UpdateUi(Action action)
    {
        if (_isClosing || Dispatcher.UIThread.CheckAccess() == false)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isClosing) return;
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"UI 更新失败: {ex.Message}");
                }
            }, DispatcherPriority.Background);
        }
        else
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"UI 更新失败: {ex.Message}");
            }
        }
    }

    private void ShowButtons(bool show)
    {
        UpdateUi(() =>
        {
            KillButton.IsEnabled = show;
        });
    }

    private async void OnKillButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_gameProcess == null)
        {
            await ShowMessage(I18nManager.Instance["Shared.Status.Error.Title"], I18nManager.Instance["LaunchWindow.Error.NoProcess"]);
            return;
        }

        try
        {
            // 尝试结束进程
            if (!_gameProcess.HasExited)
            {
                _gameProcess.Kill();
                await _gameProcess.WaitForExitAsync();
                
                ProgressBox.Text = I18nManager.Instance["LaunchWindow.Status.ProcessEnded"];
                ShowButtons(false);
                
                // 延迟关闭窗口
                await Task.Delay(1500);
                _isClosing = true;
                Close();
            }
            else
            {
                await ShowMessage(I18nManager.Instance["LaunchWindow.Notice.Title"], I18nManager.Instance["LaunchWindow.Status.ProcessEnded"]);
                ShowButtons(false);
            }
        }
        catch (Exception ex)
        {
            await ShowMessage(I18nManager.Instance["Shared.Status.Error.Title"], string.Format(I18nManager.Instance["LaunchWindow.Error.KillFailed"], ex.Message));
        }
    }

    private async Task ShowMessage(string title, string message)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // 如果使用了 MessageBox.Avalonia
            // await MessageBoxManager.GetMessageBoxStandardWindow(title, message).Show();
            
            // 临时使用 ProgressBox 显示消息
            ProgressBox.Text = $"{title}: {message}";
            await Task.Delay(2000);
        });
    }

    private Task<bool> ShowUwpChoiceAsync(List<(string Name, string Version)> depList)
    {
        var tcs = new TaskCompletionSource<bool>();
        _uwpChoiceTcs = tcs;
        _uwpMissingDeps = depList;

        UpdateUi(() =>
        {
            UwpDepsText.Text = string.Format(
                I18nManager.Instance["LaunchWindow.Status.UWP.Missing.Content"],
                string.Join("\n", depList.Select(d => d.Name)));
            UwpChoicePanel.IsVisible = true;
        });

        return tcs.Task;
    }

    private void OnUwpFixClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_uwpMissingDeps == null) return;

        UpdateUi(() =>
        {
            UwpChoicePanel.IsVisible = false;
            UwpFixHost.IsVisible = true;
            UwpFixHost.Content = new DialogDownloadUwpDependenceContent(_uwpMissingDeps)
            {
                // 补全流程结束后继续启动
                AllCompleted = () => _uwpChoiceTcs?.TrySetResult(true)
            };
        });
    }

    private void OnUwpLaunchAnywayClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _uwpChoiceTcs?.TrySetResult(true);
    }

    private void OnUwpCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _uwpChoiceTcs?.TrySetResult(false);
    }

    private async Task LaunchAsync()
    {
        if (_versionInfo == null)
        {
            UpdateUi(() => ProgressBox.Text = I18nManager.Instance["LaunchWindow.Error.InvalidVersion"]);
            return;
        }

        try
        {
            // -jump 快捷启动路径没有主窗口，平台框架（PlatformCore）未装配时先装配
            if (PlatformCore.CoreInit == null)
                await CoreInitialize.InitPlatformAsync();

#if WINDOWS
            // PreLoad.NET 组件缺失时阻塞等待下载完成，不可用则取消启动（严格守卫）
            if (!await PreLoadDependencyHelper.EnsureComponentReadyAsync())
            {
                _isClosing = true;
                Close();
                return;
            }
            UpdateUi(() => ProgressBox.Text = I18nManager.Instance["LaunchWindow.Status.PreLoad.Ready"]);

            // 启动游戏前检测一次 UWP 依赖，用户取消则不启动
            var missingDeps = await UwpDependenceLaunchGuard.GetMissingDepsIfEnabledAsync();
            if (missingDeps != null)
            {
                UpdateUi(() => ProgressBox.Text = I18nManager.Instance["LaunchWindow.Status.UWP.Checking"]);
                var proceed = await ShowUwpChoiceAsync(missingDeps);
                UpdateUi(() =>
                {
                    UwpChoicePanel.IsVisible = false;
                    UwpFixHost.IsVisible = false;
                });
                if (!proceed)
                {
                    _isClosing = true;
                    Close();
                    return;
                }
            }
#endif

            var _launcher = PlatformCore.CreateLauncher(_versionInfo);

            // 设置进度更新回调
            _launcher.UpdateProgress = (status, percentage) =>
            {
                // 节流：限制更新频率
                var now = DateTime.Now;
                if ((now - _lastUpdateTime) < _updateThrottle && percentage > 0 && percentage < 100)
                    return;
                _lastUpdateTime = now;

                UpdateUi(() =>
                {
                    ProgressBox.Text = $"{status} ({percentage:F0}%)";
                    LaunchProgressBar.Value = Math.Clamp((int)percentage, 0, 100);
                    LaunchProgressBar.IsIndeterminate = false;
                });
            };

            // 设置进度文本更新回调
            _launcher.UpdateProgressText = text =>
            {
                UpdateUi(() =>
                {
                    ProgressBox.Text = text;
                });
            };

            // 设置进度条模式回调
            _launcher.SetProgressIndeterminate = isIndeterminate =>
            {
                UpdateUi(() =>
                {
                    LaunchProgressBar.IsIndeterminate = isIndeterminate;
                    if (!isIndeterminate)
                    {
                        LaunchProgressBar.Value = 0;
                    }
                });
            };

            // 设置启动完成回调
            _launcher.LaunchCompleted = () =>
            {
                UpdateUi(() =>
                {
                    ProgressBox.Text = I18nManager.Instance["LaunchWindow.Status.Complete"];
                    // 隐藏按钮，因为游戏已经启动完成
                    ShowButtons(false);
                });
            };

            // 设置游戏启动回调
            _launcher.Launched = process =>
            {
                _gameProcess = process;
                _gameLaunched = true;
                
                Console.WriteLine($@"游戏已启动，进程ID: {process.Id}");
                UpdateUi(() =>
                {
                    ProgressBox.Text = string.Format(I18nManager.Instance["LaunchWindow.Status.Launched"], process.Id);
                    LaunchProgressBar.IsIndeterminate = false;
                    LaunchProgressBar.Value = 100;
                    
                    // 显示按钮
                    ShowButtons(true);
                });
            };

            // 启动游戏
            await _launcher.LaunchGame();
        }
        catch (Exception ex)
        {
            UpdateUi(() =>
            {
                ProgressBox.Text = string.Format(I18nManager.Instance["LaunchWindow.Error.LaunchFailed"], ex.Message);
            });
            
            // 延迟关闭
            await Task.Delay(3000);
            UpdateUi(() =>
            {
                _isClosing = true;
                Close();
            });
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // 游戏运行中也直接关闭窗口（不结束游戏）。
        // 旧实现先 e.Cancel = true 再 post 一次 Close()，而 OnClosing 没有检查 _isClosing，
        // 第二次 Close 会再次触发取消 → 无限重入，UI 线程被调度任务占满直接卡死。
        _isClosing = true;
        base.OnClosing(e);
    }

    // 添加资源清理
    protected override void OnClosed(EventArgs e)
    {
        // 不结束游戏进程，游戏继续在后台运行
        base.OnClosed(e);

        // -jump 快捷启动没有其他窗口，本窗口关闭即应用生命周期结束；
        // 显式退出进程，与主窗口的退出行为保持一致（内存监视线程等常驻线程不应拖住进程）
        Environment.Exit(0);
    }
}