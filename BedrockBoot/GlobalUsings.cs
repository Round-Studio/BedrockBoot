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

public static class global_cfg
{
    public static BedrockCore core = new BedrockCore();
    public static DownloadPage _downloadPage;
    public static ObservableCollection<TaskCard> tasksPool = new ObservableCollection<TaskCard>();
    public static string AppxDir = "./";
    public static void InstallTasksAsync(string taskname,string installdir,VersionInformation ver)
    {
        var taskCard = new TaskCard()
        {
            Version = ver
        };
        taskCard.DoInstallAsync(taskname,installdir,Path.Combine(Directory.GetCurrentDirectory(),ver.ID+".appx"));
        tasksPool.Add(taskCard);
    }
}