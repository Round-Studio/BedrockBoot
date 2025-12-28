using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Enum;
using Round.SDK.Global;
using Round.SDK.Logger;

namespace BedrockBoot;

sealed class Program
{
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    // Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
    public static void Main(string[] args)
    {
        // 首先处理可能的更新参数（--update-launcher, --update-replace）
        AppUpdater.ProcessStartupArgs(args);
        
        PluginEnvironment.RunningProduct = ProductEnum.BedrockBoot;

    
        // 然后处理原有的 -update 参数
        if (args.Length > 0)
        {
            if (!ArgsAnalytical(args.ToList()))
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
        }
        else
        {
            ConsoleRedirector consoleRedirector = new ConsoleRedirector(Path.Combine(PathsList.LogPath,
                $"[BedrockBoot.Logger] {DateTime.Now.ToString("yyyy.MM.dd HHmmss.fff")}.log"));
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    private static bool ArgsAnalytical(List<string> args)
    {
        var result = false;
        args.ForEach((arg) =>
        {
            switch (arg)
            {
                case "-update":
                    Console.WriteLine("触发更新，本次启动将不会拉起窗体。");
                    // 修改为调用新的更新方法
                    AppUpdater.StartUpdateFromOldVersion(args[args.FindIndex(x => x == "-update") + 1]);
                    result = true;
                    break;
                case "-shell":
                    // 查找 -shell 参数的索引
                    int shellIndex = args.FindIndex(x => x == "-shell");
    
                    // 检查是否提供了命令参数
                    if (shellIndex + 1 >= args.Count)
                    {
                        Console.WriteLine("错误：-shell 参数后需要指定命令");
                        break;
                    }
    
                    string command = args[shellIndex + 1];
                    Console.WriteLine($"触发 bb 协议：{command}");
    
                    try
                    {
                        Sent(command);
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"请求出错：{ex.Message}");

                        Task.Run(() =>
                        {
                            Thread.Sleep(2000);
                            Sent(command);
                        });
                    }
                    break;
                case "-console":
                    AllocConsole();
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.WriteLine("已开启 Release 中的 Debug 模式，此模式不会生成日志！");
                    break;
            }
        });

        return result;
    }

    private static void Sent(string command)
    {
        // 创建 HttpClientHandler 来处理自签名证书
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        
        // 使用 HttpClient
        using var httpClient = new HttpClient(handler);
        
        // URL 编码命令参数
        string encodedCommand = Uri.EscapeDataString(command);
        string url = $"http://127.0.0.1:43956/shell?command={encodedCommand}";
        
        // 异步发送请求
        var response = httpClient.GetAsync(url).Result;
        
        // 如果需要，可以读取响应
        if (response.IsSuccessStatusCode)
        {
            string responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"服务器响应：{responseContent}");
        }
        else
        {
            Console.WriteLine($"请求失败，状态码：{response.StatusCode}");
        }
    }

    public static void StartUpdate(string oldVersionPath)
    {
        try
        {
            if (!File.Exists(oldVersionPath))
            {
                Console.WriteLine("旧版本文件不存在，无需更新");
                return;
            }

            // 获取当前程序路径
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
            {
                Console.WriteLine("无法获取当前程序路径");
                return;
            }

            // 检查是否是同一个文件
            if (string.Equals(currentExePath, oldVersionPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("新旧文件路径相同，无需更新");
                return;
            }

            Console.WriteLine($"开始更新：从 {currentExePath} 到 {oldVersionPath}");

            // 1. 复制新版本到临时位置
            string tempFile = oldVersionPath + ".new";
            File.Copy(currentExePath, tempFile, true);

            // 2. 启动新版本（从临时文件）
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = tempFile,
                Arguments = $"--updated \"{oldVersionPath}\"", // 传递原文件路径
                UseShellExecute = true
            };
            Process.Start(startInfo);

            // 3. 退出当前程序
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新失败: {ex.Message}");
            // 可以选择重新抛出或记录日志
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}