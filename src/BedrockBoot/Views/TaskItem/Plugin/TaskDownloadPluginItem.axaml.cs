using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Pack.Market;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Plugin;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.TaskItem.Plugin;

public partial class TaskDownloadPluginItem : UserControl
{
    private readonly Release _release;
    private readonly MarketResponse.PluginInfo _pluginInfo;
    public Action? Finish;

    public TaskDownloadPluginItem()
    {
        InitializeComponent();
    }
    
    public TaskDownloadPluginItem(Release release, MarketResponse.PluginInfo pluginInfo):this()
    {
        _release = release;
        _pluginInfo = pluginInfo;
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    public void Install(Action finish)
    {
        Finish = finish;
        var fileList = new List<string>();
        _release.Assets.ToList().ForEach(async x =>
        {
            var fileUrl = x.BrowserDownloadUrl;
            var randomFolder = Path.Combine(PathsList.TempPath, Guid.NewGuid().ToString("N"));
            var fileName = Path.Combine(randomFolder, $"{_pluginInfo.Username}.{_pluginInfo.PluginName}.({Path.GetFileName(fileUrl)})");

            await new GithubFilesDownloader().DownloadAsync(fileUrl, fileName, new Progress<DownloadProgress>(p =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressText.Text = $"{p.ProgressPercentage:F2} %";
                    ProgressBar.Value = (int)p.ProgressPercentage;
                    ProgressBar.IsIndeterminate = false;
                });
            }));

            fileList.Add(fileName);

            if (fileList.Count == _release.Assets.Count)
            {
                fileList.ForEach(f =>
                {
                    var conf = PluginHelper.ReadPackConfig(fileName);
                    var newFileName = Path.Combine(randomFolder,
                        $"{_pluginInfo.Username}.{_pluginInfo.PluginName}@{conf.PackVersion}({Path.GetFileName(fileUrl)}).rplck");
                    File.Copy(f, newFileName, true);
                    PluginLoader.Install(newFileName).Wait();
                });
                Finish?.Invoke();
            }
        });
    }

    public static void Install(Release release, MarketResponse.PluginInfo pluginInfo)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "下载插件包",
            Message = "插件包已开始下载",
            NoticeType = NoticeType.Info
        });

        var body = new TaskDownloadPluginItem(release, pluginInfo);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }
}