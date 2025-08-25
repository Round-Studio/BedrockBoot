global using DevWinUI;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
using BedrockBoot;
using BedrockBoot.Controls;
using BedrockBoot.Pages;
using BedrockBoot.Versions;
using BedrockLauncher.Core;
using BedrockLauncher.Core.JsonHandle;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public static class global_cfg
{
    public static BedrockCore core = new BedrockCore();
    public static DownloadPage _downloadPage;
    public static ObservableCollection<TaskExpander> tasksPool = new ObservableCollection<TaskExpander>();
    public static List<Process> MinecraftProcesses = new List<Process>();
    public static Config cfg = null;
    public static MainWindow MainWindow;
    public static int selectIndex;

    public static void InitConfig()
    {
        cfg = new Config();
    }
    public static void InstallTasksAsync(string taskname,string installdir,string backColor,string backImg,VersionInformation ver,string Appx_dir,bool UseAppx = false)
    {
        var taskCard = new TaskExpander()
        {
            Version = ver
        };
        taskCard.Header = taskname;
        taskCard.DoInstallAsync(taskname,installdir,Path.Combine(Appx_dir,cfg.JsonCfg.appxName.Replace("{0}",ver.ID)),backColor, backImg,UseAppx);
        tasksPool.Add(taskCard);
    }
}
public class DllFileInfo
{
    public string FullPath { get; set; }
    public string FileName { get; set; }
    public string Directory { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public long FileSize { get; set; } // 文件大小（字节）

    public override string ToString()
    {
        return $"{FileName} - {LastWriteTime:yyyy-MM-dd HH:mm:ss} - {FullPath}";
    }
}
public static class globalTools
{
    public static T GetJsonFileEntry<T>(string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException();
        return JsonSerializer.Deserialize<T>(File.ReadAllText(file));
    }

    public static void SearchVersionJson(string currentPath1,ref List<string> textList, int currentDepth, int maxDepth)
    {
        var currentPath = Path.GetFullPath(currentPath1);
        try
        {
            if (!Directory.Exists(currentPath))
            {
                return;
            }
            // 检查是否超出最大深度
            if (currentDepth > maxDepth)
            {
                return ;
            }

            // 搜索当前目录下的 version.json 文件
            string[] files = Directory.GetFiles(currentPath, "version.json", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                textList.Add(file);
            }

            // 如果未达到最大深度，继续搜索子目录
            if (currentDepth < maxDepth)
            {
                try
                {
                    string[] subDirs = Directory.GetDirectories(currentPath);
                    foreach (string dir in subDirs)
                    {
                        SearchVersionJson(dir,ref textList,currentDepth + 1, maxDepth);
                    }
                }
                catch 
                {
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public static void ShowInfo(string text)
    {
        App._window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            Growl.InfoGlobal(new GrowlInfo
            {
                ShowDateTime = true,
                StaysOpen = true,
                IsClosable = true,
                Title = "Info",
                Message = text,
                UseBlueColorForInfo = true,
                WaitTime = new TimeSpan(0, 0, 0, 5)
            });
        });
    }
    public static List<DllFileInfo> GetDllFiles(string directoryPath,
                                               bool includeSubdirectories = true,
                                               bool sortByLastWriteTime = true)
    {
        var dllFiles = new List<DllFileInfo>();

        try
        {
            // 验证目录
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("目录路径不能为空", nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // 设置搜索选项
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 获取所有 .dll 文件
            var dllFilePaths = Directory.GetFiles(directoryPath, "*.dll", searchOption);

            // 处理每个文件
            foreach (var filePath in dllFilePaths)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);

                    dllFiles.Add(new DllFileInfo
                    {
                        FullPath = fileInfo.FullName,
                        FileName = fileInfo.Name,
                        Directory = fileInfo.DirectoryName,
                        CreationTime = fileInfo.CreationTime,
                        LastWriteTime = fileInfo.LastWriteTime,
                        LastAccessTime = fileInfo.LastAccessTime,
                        FileSize = fileInfo.Length
                    });
                }
                catch (Exception fileEx)
                {
                    // 处理单个文件访问异常（如权限问题）
                    Console.WriteLine($"无法访问文件 {filePath}: {fileEx.Message}");
                    continue;
                }
            }

            // 排序
            if (sortByLastWriteTime)
            {
                dllFiles = dllFiles.OrderByDescending(f => f.LastWriteTime).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取 DLL 文件信息时出错: {ex.Message}");
            throw;
        }
        return dllFiles;
    }
}