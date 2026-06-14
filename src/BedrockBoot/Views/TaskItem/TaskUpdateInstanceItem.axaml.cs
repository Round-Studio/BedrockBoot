using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum.Type.Progress.Steps;
using BedrockBoot.Models.Pack.Game.Instance;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskUpdateInstanceItem : UserControl
{
    private readonly VersionConfig _versionConfig;
    private readonly BuildInfo _buildInfo;
    private readonly string _selectedUrl;

    public TaskUpdateInstanceItem()
    {
        InitializeComponent();
    }

    public TaskUpdateInstanceItem(VersionConfig versionConfig, BuildInfo buildInfo, string selectedUrl) : this()
    {
        _versionConfig = versionConfig;
        _buildInfo = buildInfo;
        _selectedUrl = selectedUrl;
    }

    public void Start(Action completed)
    {
        CardTitle.Text = $"更新 {_versionConfig.Info.VersionName} 到 {_buildInfo.ID}";

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
                break;
            case InstanceUpdateStep.Download:
                InsDownBar.IsIndeterminate = false;
                InsDownBar.Value = p.Progress;
                break;
            case InstanceUpdateStep.UnZip:
                InsUnZipBar.IsIndeterminate = false;
                InsUnZipBar.Value = p.Progress;
                break;
            case InstanceUpdateStep.UWPRegistering:
                InsRegBar.IsIndeterminate = false;
                InsRegBar.Value = p.Progress;
                break;
            case InstanceUpdateStep.UpdateFinish:
                MainProgressBar.IsIndeterminate = false;
                MainProgressBar.Value = 100;
                break;
        }
    }
}
