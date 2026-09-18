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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Models.Global;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem.Linux.Proton;

public partial class TaskDownloadProtonItem : UserControl, ITaskItem
{
    private readonly ProtonInfo _info;
    private readonly InstallInfo _installInfo;
    
    public Action? CallBack { get; set; }
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

    public TaskDownloadProtonItem()
    {
        InitializeComponent();
    }

    public TaskDownloadProtonItem(ProtonInfo info, InstallInfo installInfo) : this()
    {
        _info = info;
        _installInfo = installInfo;
        _taskTitle = "下载 Proton";
    }

    public void Install()
    {
        Task.Run(async () =>
        {
            await ProtonCore.InstallProton(_info, _installInfo, new Progress<DownloadProgress>(p =>
            {
                ReportProgress(p.ProgressPercentage, $"{p.Message} ({p.ProgressPercentage:F2} %)");
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressText.Text = $"{p.Message} ({p.ProgressPercentage:F2} %)";
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = (int)p.ProgressPercentage;
                });
            }));

            ReportProgress(100, "安装完成");
            Dispatcher.UIThread.Invoke(CallBack!);
        });
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {

    }

    public static void Install(ProtonInfo info, InstallInfo installInfo)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "下载 Proton",
            Message = "已添加下载任务至任务列表",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadProtonItem(info, installInfo);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.CallBack = () =>
        {
            GlobalModel.TaskManager.RemoveTask(tuid);
            GameProton.UpdateList?.Invoke();
        };
        body.Install();
    }
}