global using DevWinUI;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
using BedrockBoot.Controls;
using BedrockBoot.Pages;
using BedrockLauncher.Core;
using BedrockLauncher.Core.JsonHandle;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot;

public static class global_cfg
{
    public static BedrockCore core = new BedrockCore();
    public static DownloadPage _downloadPage;
    public static ObservableCollection<TaskExpander> tasksPool = new ObservableCollection<TaskExpander>();
    public static Config cfg = new Config();
    public static void InstallTasksAsync(string taskname,string installdir,VersionInformation ver)
    {
        var taskCard = new TaskExpander()
        {
            Version = ver
        };
        taskCard.Header = taskname;
        taskCard.DoInstallAsync(taskname,installdir,Path.Combine(cfg.JsonCfg.appxDir,cfg.JsonCfg.appxName));
        tasksPool.Add(taskCard);
    }
}