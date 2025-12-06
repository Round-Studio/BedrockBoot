using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Enum;
using Round.SDK.Global;
using Round.SDK.Logger;

namespace BedrockBoot;

sealed class Program
{

    // Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
    public static void Main(string[] args)
    {
        PluginEnvironment.RunningProduct = ProductEnum.BedrockBoot;
		Console.OutputEncoding = Encoding.UTF8;
        GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
        GlobalModel.Config.Load();
    
        ConsoleRedirector consoleRedirector = new ConsoleRedirector(Path.Combine(PathsList.LogPath,
            $"[BedrockBoot.Logger] {DateTime.Now.ToString("yyyy.MM.dd HHmmss.fff")}.log"));
    
        Console.WriteLine($"启动参数长度：{args.Length}");
    
        // 首先处理可能的更新参数（--update-launcher, --update-replace）
        AppUpdater.ProcessStartupArgs(args);
    
        // 然后处理原有的 -update 参数
        if (args.Length > 0)
            ArgsAnalytical(args.ToList());

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ArgsAnalytical(List<string> args)
    {
        args.ForEach(arg =>
        {
            switch (arg)
            {
                case "-update":
                    Console.WriteLine("触发更新，本次启动将不会拉起窗体。");
                    // 修改为调用新的更新方法
                    AppUpdater.StartUpdateFromOldVersion(args[args.FindIndex(x => x == "-update") + 1]);
                    break;
            }
        });
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