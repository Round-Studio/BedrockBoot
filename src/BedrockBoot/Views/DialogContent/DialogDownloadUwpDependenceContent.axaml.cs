using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
#if WINDOWS
using BedrockBoot.Models.Helper.Uwp;
#endif

namespace BedrockBoot.Views.DialogContent;

public partial class DialogDownloadUwpDependenceContent : UserControl
{
    private readonly List<(string Name, string Version)> _deps;
    private readonly List<ProgressBar> _progressBars = new();
    private readonly HashSet<int> _installed = new(); // 记录已安装的包
    private int _completedCount;

    public DialogDownloadUwpDependenceContent()
    {
        InitializeComponent();
    }

    public DialogDownloadUwpDependenceContent(List<(string, string)> deps) : this()
    {
        _deps = deps;
        BuildUI();
        _ = StartAllDownloads();
    }

    private void BuildUI()
    {
        foreach (var dep in _deps)
        {
            var stack = new StackPanel { Spacing = 4};
            
            stack.Children.Add(new TextBlock 
            { 
                Text = dep.Name
            });
            
            var progressBar = new ProgressBar 
            { 
                Width = 300, 
                Minimum = 0,
                Maximum = 100
            };
            _progressBars.Add(progressBar);
            stack.Children.Add(progressBar);
            
            StackPanel.Children.Add(stack);
        }
    }

    private async Task StartAllDownloads()
    {
        // 1. 获取所有下载地址
        var downloadTasks = new List<Task<(string Name, string Url, int Index)>>();
        for (int i = 0; i < _deps.Count; i++)
        {
            downloadTasks.Add(GetUrlAsync(_deps[i].Name, _deps[i].Version, i));
        }
    
        var urls = await Task.WhenAll(downloadTasks);
    
        var validUrls = urls.Where(u => !string.IsNullOrEmpty(u.Url)).ToArray();
    
        if (validUrls.Length == 0)
        {
            Dispatcher.UIThread.Invoke(() => {
                StackPanel.Children.Add(new TextBlock { Text = "未找到任何有效的下载地址, 请重启启动器！", Foreground = Avalonia.Media.Brushes.Red });
            });
            return;
        }

        var tasks = validUrls.Select(urlInfo => DownloadOneAsync(urlInfo.Name, urlInfo.Url, urlInfo.Index)).ToArray();
        await Task.WhenAll(tasks);

        Dispatcher.UIThread.Invoke(() =>
        {
            DialogHost.Close();
        });
    }
    
    private async Task<(string Name, string Url, int Index)> GetUrlAsync(string name, string version, int index)
    {
#if WINDOWS
        var url = await UwpFileUrl.GetUwpPackageDownloadUrl(name, version);
        return (name, url, index);
#endif

        return (null,null,0);
    }

    private async Task DownloadOneAsync(string name, string url, int index)
    {
        if (string.IsNullOrEmpty(url))
        {
            Console.WriteLine($@"获取 {name} 下载地址失败");
            return;
        }
    
        string tempFile = null;
        try
        {
            tempFile = Path.Combine(PathsList.TempPath, $"{name}_{Guid.NewGuid()}.appx");
        
            var progress = new Progress<DownloadProgress>();
            progress.ProgressChanged += (_, p) =>
            {
                var percent = p.TotalBytes > 0 ? (double)p.DownloadedBytes / p.TotalBytes * 100 : 0;
                Dispatcher.UIThread.Invoke(() => _progressBars[index].Value = percent);
            };

            using var downloader = new MultiThreadDownloader();
            await downloader.DownloadAsync(url, tempFile, progress);
        
            Dispatcher.UIThread.Invoke(() => _progressBars[index].Value = 100);
            await InstallPackageAsync(tempFile, index);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"下载或安装失败 {name}: {ex.Message}");
        }
        finally
        {
            _completedCount++;
        }
    }

    private async Task InstallPackageAsync(string filePath, int index)
    {
        if (_installed.Contains(index)) return;
        _installed.Add(index);
    
        await Task.Run(() =>
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Add-AppxPackage -Path '{filePath}'\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
        
            using var process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();
        
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                Console.WriteLine($@"安装失败: {error}");
            }
        });
    }
}