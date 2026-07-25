using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Base.Enum.Type.Progress.Steps;
using BedrockBoot.Models.Pack.Game.Instance;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskUpdateInstanceItem : UserControl, ITaskItem
{
    private readonly VersionConfig _versionConfig;
    private readonly BuildInfo _buildInfo;
    private readonly string _selectedUrl;
    private double _taskProgress;
    private string _taskStatusText = "";
    private string _taskTitle = "";
    private bool _taskIsCompleted;
    private bool _taskIsIndeterminate = true;
    private Action? Completed;

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

    public TaskUpdateInstanceItem()
    {
        InitializeComponent();
    }

    public TaskUpdateInstanceItem(VersionConfig versionConfig, BuildInfo buildInfo, string selectedUrl) : this()
    {
        _versionConfig = versionConfig;
        _buildInfo = buildInfo;
        _selectedUrl = selectedUrl;
        _taskTitle = $"更新 {_versionConfig.Info.VersionName} 到 {_buildInfo.ID}";
    }

    public void Start(Action completed)
    {
        Completed = completed;
        CardTitle.Text = _taskTitle;

        var progress = new Progress<InstanceUpdateProgress>(p =>
        {
            Dispatcher.UIThread.Invoke(() => UpdateProgress(p));
        });

        var updater = new InstanceUpdater(_versionConfig)
        {
            ChooseDownloadUrl = _ => _selectedUrl,
            Progress = progress
        };

        Task.Run(async () =>
        {
            try
            {
                await updater.UpdateAsync(_buildInfo);
                Dispatcher.UIThread.Invoke(() =>
                {
                    MainText.Text = "更新完成";
                    MainSpeedText.Text = "";
                    MainProgressBar.IsIndeterminate = false;
                    MainProgressBar.Value = 100;
                });
                completed?.Invoke();
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    MainText.Text = $"更新失败: {ex.Message}";
                });
            }
        });
    }

    private void UpdateProgress(InstanceUpdateProgress p)
    {
        MainText.Text = p.Message;
        MainSpeedText.Text = p.Detailed;

        switch (p.Step)
        {
            case InstanceUpdateStep.DeleteOld:
                InsDelBar.IsIndeterminate = false;
                InsDelBar.Value = p.Progress;
                ReportProgress(p.Progress * 0.25, p.Message);
                break;
            case InstanceUpdateStep.Download:
                InsDownBar.IsIndeterminate = false;
                InsDownBar.Value = p.Progress;
                ReportProgress(25 + p.Progress * 0.25, p.Message);
                break;
            case InstanceUpdateStep.UnZip:
                InsUnZipBar.IsIndeterminate = false;
                InsUnZipBar.Value = p.Progress;
                ReportProgress(50 + p.Progress * 0.25, p.Message);
                break;
            case InstanceUpdateStep.UWPRegistering:
                InsRegBar.IsIndeterminate = false;
                InsRegBar.Value = p.Progress;
                ReportProgress(75 + p.Progress * 0.25, p.Message);
                break;
            case InstanceUpdateStep.UpdateFinish:
                MainProgressBar.IsIndeterminate = false;
                MainProgressBar.Value = 100;
                ReportProgress(100, p.Message);
                Completed?.Invoke();
                break;
        }
    }
}
