using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Desktop;
using BedrockBoot.Models.Game;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Diagnostics;

namespace BedrockBoot.Views.Windows.SystemMethod;

public partial class LaunchGameWindow : Window
{
    private readonly VersionConfig? _versionInfo;
    private EasyLauncher? _launcher;
    private bool _isClosing;
    private DateTime _lastUpdateTime;
    private readonly TimeSpan _updateThrottle = TimeSpan.FromMilliseconds(100);
    private Process? _gameProcess;
    private bool _gameLaunched;

    public LaunchGameWindow()
    {
        InitializeComponent();
        
        try
        {
            if (Program.Args == null || !Program.Args.Contains("-jump"))
                throw new Exception("无效的启动参数");

            var index = Program.Args.FindIndex(a => a == "-jump");
            if (index + 1 >= Program.Args.Count)
                throw new Exception("缺少版本路径参数");

            var folder = Program.Args[index + 1];
            if (!Directory.Exists(folder))
                throw new Exception("路径不存在");

            _versionInfo = GameInfoHelper.GetVersionConfig(folder);
            
            if (_versionInfo?.Info?.VersionName != null)
            {
                GameNameBox.Text = $"启动游戏 {_versionInfo.Info.VersionName}";
            }
        }
        catch (Exception ex)
        {
            ProgressBox.Text = $"错误: {ex.Message}";
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
                    Console.WriteLine($"UI 更新失败: {ex.Message}");
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
                Console.WriteLine($"UI 更新失败: {ex.Message}");
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
            await ShowMessage("错误", "没有正在运行的游戏进程");
            return;
        }

        try
        {
            // 尝试结束进程
            if (!_gameProcess.HasExited)
            {
                _gameProcess.Kill();
                await _gameProcess.WaitForExitAsync();
                
                ProgressBox.Text = "游戏进程已结束";
                ShowButtons(false);
                
                // 延迟关闭窗口
                await Task.Delay(1500);
                _isClosing = true;
                Close();
            }
            else
            {
                await ShowMessage("提示", "游戏进程已经结束");
                ShowButtons(false);
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("错误", $"结束进程失败: {ex.Message}");
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

    private async Task LaunchAsync()
    {
        if (_versionInfo == null)
        {
            UpdateUi(() => ProgressBox.Text = "版本信息无效");
            return;
        }

        try
        {
            _launcher = new EasyLauncher(_versionInfo);

            // 设置迁移回调
            _launcher.OnMigration = () =>
            {
                UpdateUi(() =>
                {
                    ProgressBox.Text = "需要迁移，请使用完整启动器";
                    _isClosing = true;
                    Close();
                });
            };

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
                    ProgressBox.Text = "启动完成";
                    // 隐藏按钮，因为游戏已经启动完成
                    ShowButtons(false);
                });
            };

            // 设置游戏启动回调
            _launcher.Launched = process =>
            {
                _gameProcess = process;
                _gameLaunched = true;
                
                Console.WriteLine($"游戏已启动，进程ID: {process.Id}");
                UpdateUi(() =>
                {
                    ProgressBox.Text = $"游戏已启动 (PID: {process.Id})";
                    LaunchProgressBar.IsIndeterminate = false;
                    LaunchProgressBar.Value = 100;
                    
                    // 显示按钮
                    ShowButtons(true);
                });
            };

            // 启动游戏
            await _launcher.Launch();
        }
        catch (Exception ex)
        {
            UpdateUi(() =>
            {
                ProgressBox.Text = $"启动失败: {ex.Message}";
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
        // 如果游戏还在运行，询问用户是否要结束
        if (_gameProcess != null && !_gameProcess.HasExited && _gameLaunched)
        {
            // 由于 Avalonia 没有内置的 MessageBox，这里使用简单方式
            // 或者使用 MessageBox.Avalonia
            e.Cancel = true;
            
            // 显示确认对话框
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // 使用 MessageBox.Avalonia
                // var result = await MessageBoxManager.GetMessageBoxStandardWindow(
                //     "确认关闭",
                //     "游戏正在运行，确定要关闭窗口吗？游戏将继续在后台运行。",
                //     ButtonEnum.YesNo,
                //     Icon.Question).Show();
                // 
                // if (result == ButtonResult.Yes)
                // {
                //     _isClosing = true;
                //     Close();
                // }
                
                // 临时方案：直接关闭，不结束游戏
                _isClosing = true;
                Close();
            });
        }
        else
        {
            _isClosing = true;
            base.OnClosing(e);
        }
    }

    // 添加资源清理
    protected override void OnClosed(EventArgs e)
    {
        // 如果游戏还在运行，不结束进程（让用户选择是否结束）
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            // 可以选择是否结束进程
            // _gameProcess.Kill();
        }
        
        base.OnClosed(e);
    }
}