using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent;

public class DownloadFileTask
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string SavePath { get; set; }
    public Action<string> OnComplete { get; set; }
}

public partial class DialogDownloadManagerContent : UserControl
{
    private StackPanel _mainPanel;
    private ProgressBar _mainProgressBar;
    private TextBlock _mainProgressText;
    private TextBlock _statusText;
    private Dictionary<string, ProgressBar> _progressBars = new();
    private Dictionary<string, TextBlock> _progressTexts = new();
    private int _totalTasks;
    private int _completedTasks;
    private bool _isCompleted;
    private Action<List<string>> _onAllComplete;
    private List<string> _downloadedFiles = new();
    private List<string> _failedFiles = new();

    public DialogDownloadManagerContent()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        _mainPanel = new StackPanel();

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var progressRing = new ProgressRing
        {
            Height = 36,
            Width = 36,
            Background = Avalonia.Media.Brushes.Transparent
        };

        var textStack = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _mainProgressText = new TextBlock
        {
            FontSize = 16,
            Text = "准备下载..."
        };

        _mainProgressBar = new ProgressBar
        {
            IsIndeterminate = true,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _statusText = new TextBlock
        {
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Gray,
            Margin = new Thickness(0, 4, 0, 0),
            Text = "初始化..."
        };

        textStack.Children.Add(_mainProgressText);
        textStack.Children.Add(_mainProgressBar);
        textStack.Children.Add(_statusText);
        headerPanel.Children.Add(progressRing);
        headerPanel.Children.Add(textStack);
        _mainPanel.Children.Add(headerPanel);

        Content = _mainPanel;
    }

    private void AddTaskUI(string taskName)
    {
        var rowPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4)
        };

        var nameText = new TextBlock
        {
            Text = taskName,
            Width = 180,
            VerticalAlignment = VerticalAlignment.Center
        };

        var progressBar = new ProgressBar
        {
            MinWidth = 160,
            Margin = new Thickness(8, 0),
            IsIndeterminate = true
        };

        var progressText = new TextBlock
        {
            Text = "等待中",
            Width = 80,
            Margin = new Thickness(8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };

        rowPanel.Children.Add(nameText);
        rowPanel.Children.Add(progressBar);
        rowPanel.Children.Add(progressText);

        Dispatcher.UIThread.Invoke(() => { _mainPanel.Children.Add(rowPanel); });

        _progressBars[taskName] = progressBar;
        _progressTexts[taskName] = progressText;
    }

    public void UpdateProgress(string taskName, double percentage, string status = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_progressBars.TryGetValue(taskName, out var bar))
            {
                bar.IsIndeterminate = false;
                bar.Value = percentage;
            }

            if (_progressTexts.TryGetValue(taskName, out var text))
            {
                if (status != null)
                {
                    text.Text = status;
                }
                else
                {
                    text.Text = $"{percentage:F1}%";
                }
            }
        });
    }

    public void SetTaskComplete(string taskName, string filePath)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_progressBars.TryGetValue(taskName, out var bar))
            {
                bar.IsIndeterminate = false;
                bar.Value = 100;
            }

            if (_progressTexts.TryGetValue(taskName, out var text))
            {
                text.Text = "完成";
            }

            _downloadedFiles.Add(filePath);
            _completedTasks++;
            UpdateMainProgress();
        });
    }

    public void SetTaskFailed(string taskName, string error = "失败")
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_progressTexts.TryGetValue(taskName, out var text))
            {
                text.Text = error;
                text.Foreground = Avalonia.Media.Brushes.Red;
            }

            if (_progressBars.TryGetValue(taskName, out var bar))
            {
                bar.IsIndeterminate = false;
                bar.Value = 0;
            }

            _failedFiles.Add(taskName);
            _completedTasks++;
            UpdateMainProgress();
        });
    }

    private void UpdateMainProgress()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_totalTasks > 0)
            {
                var totalProgress = (_completedTasks * 100.0) / _totalTasks;
                _mainProgressBar.IsIndeterminate = false;
                _mainProgressBar.Value = totalProgress;
                _mainProgressText.Text = $"总进度 ({totalProgress:F1}%)";
            }

            if (_completedTasks >= _totalTasks && _totalTasks > 0)
            {
                _isCompleted = true;
                _statusText.Text = _failedFiles.Count > 0 ? $"完成，{_failedFiles.Count} 个任务失败" : "所有任务已完成";
                _mainProgressText.Text = _failedFiles.Count > 0 ? "下载完成（有失败）" : "下载完成";
                _onAllComplete?.Invoke(_downloadedFiles);
            }
        });
    }

    public void SetStatus(string status)
    {
        Dispatcher.UIThread.Invoke(() => { _statusText.Text = status; });
    }

    public async Task StartDownloadAsync(
        List<DownloadFileTask> filesToDownload,
        Func<string, string, IProgress<DownloadProgress>, Task<bool>> downloadFunc,
        Action<List<string>> onAllComplete = null)
    {
        _onAllComplete = onAllComplete;
        _totalTasks = filesToDownload.Count;
        _completedTasks = 0;
        _downloadedFiles.Clear();
        _failedFiles.Clear();

        foreach (var file in filesToDownload)
        {
            AddTaskUI(file.Name);
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            _mainProgressBar.IsIndeterminate = false;
            _mainProgressBar.Value = 0;
            _mainProgressText.Text = $"共 {_totalTasks} 个任务";
            _statusText.Text = "开始下载...";
        });

        await Task.Run(async () =>
        {
            var tasks = new List<Task>();

            foreach (var file in filesToDownload)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(file.SavePath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        SetStatus($"正在下载 {file.Name}...");

                        var progress = new Progress<DownloadProgress>(p =>
                        {
                            UpdateProgress(file.Name, p.ProgressPercentage);
                        });

                        var success = await downloadFunc(file.Url, file.SavePath, progress);

                        if (success)
                        {
                            SetTaskComplete(file.Name, file.SavePath);
                            file.OnComplete?.Invoke(file.SavePath);
                        }
                        else
                        {
                            SetTaskFailed(file.Name, "下载失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        SetTaskFailed(file.Name, ex.Message);
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        });
    }
}