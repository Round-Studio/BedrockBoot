using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace BedrockBoot;

/// <summary>
///     应用程序更新器
/// </summary>
public static class AppUpdater
{
    private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    /// <summary>
    ///     主入口：根据参数解析决定是执行更新流程还是正常启动
    /// </summary>
    public static void ProcessStartupArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--update-launcher":
                    // 模式1：作为更新引导程序启动
                    if (i + 1 < args.Length)
                    {
                        var targetPath = args[i + 1];
                        LaunchUpdateReplacement(targetPath);
                        Environment.Exit(0);
                    }
                    break;
                case "--update-replace":
                    // 模式2：作为替换程序启动
                    if (i + 1 < args.Length)
                    {
                        var oldPath = args[i + 1];
                        PerformFileReplacement(oldPath);
                        Environment.Exit(0);
                    }
                    break;
            }
    }

    /// <summary>
    ///     从旧版本主程序启动更新
    /// </summary>
    public static void StartUpdateFromOldVersion(string oldVersionFullPath)
    {
        try
        {
            Console.WriteLine($@"开始更新流程，目标文件: {oldVersionFullPath}");

            if (!File.Exists(oldVersionFullPath))
            {
                Console.WriteLine(@"旧版本文件不存在，无法更新");
                return;
            }

            var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExePath) || !File.Exists(currentExePath))
            {
                Console.WriteLine(@"无法获取当前程序路径");
                return;
            }

            // 检查是否试图自我更新
            if (Path.GetFullPath(currentExePath).Equals(
                    Path.GetFullPath(oldVersionFullPath), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(@"新旧文件路径相同，跳过更新");
                return;
            }

            // 关键步骤1：启动更新引导程序（当前程序的新实例）
            var launcherInfo = new ProcessStartInfo
            {
                FileName = currentExePath,
                Arguments = $"--update-launcher \"{oldVersionFullPath}\"",
                UseShellExecute = !IsLinux, // Linux 上设置为 false
                WindowStyle = ProcessWindowStyle.Normal
            };
            
            // Linux 特殊处理
            if (IsLinux)
            {
                launcherInfo.UseShellExecute = false;
                launcherInfo.CreateNoWindow = true;
            }

            Console.WriteLine($@"启动更新引导程序: {currentExePath}");
            Process.Start(launcherInfo);

            Console.WriteLine(@"更新引导程序已启动，当前进程即将退出");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动更新流程失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     作为更新引导程序启动：复制自身并启动替换程序
    /// </summary>
    private static void LaunchUpdateReplacement(string targetPath)
    {
        try
        {
            var currentPath = Process.GetCurrentProcess().MainModule?.FileName;
            var tempDir = Path.GetTempPath();
            var tempFileName = IsLinux 
                ? $"BedrockBoot_Update_{Guid.NewGuid():N}"
                : $"BedrockBoot_Update_{Guid.NewGuid():N}.exe";
            var tempPath = Path.Combine(tempDir, tempFileName);

            Console.WriteLine($@"引导程序：复制到临时位置 {tempPath}");

            // 复制当前程序到临时位置
            File.Copy(currentPath, tempPath, true);
            
            // Linux: 设置可执行权限
            if (IsLinux)
            {
                var chmodProcess = Process.Start("chmod", $"+x \"{tempPath}\"");
                chmodProcess?.WaitForExit();
            }

            // 关键步骤2：启动临时副本作为替换程序
            var replaceInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = $"--update-replace \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Console.WriteLine($@"启动替换程序来更新 {targetPath}");
            Process.Start(replaceInfo);

            Console.WriteLine(@"更新引导程序退出");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"引导更新失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     作为替换程序执行文件替换操作
    /// </summary>
    private static void PerformFileReplacement(string oldPath)
    {
        try
        {
            var currentPath = Process.GetCurrentProcess().MainModule?.FileName;

            Console.WriteLine($@"替换程序：准备替换 {oldPath}");

            // 确保原进程已退出，重试多次
            var replaced = false;
            for (var i = 0; i < 5; i++)
                try
                {
                    // 关键步骤3：执行文件替换
                    File.Delete(oldPath);
                    File.Move(currentPath, oldPath);
                    replaced = true;
                    Console.WriteLine($@"文件替换成功 (第{i + 1}次尝试)");
                    break;
                }
                catch (IOException ioEx) when (i < 4)
                {
                    Console.WriteLine($@"文件被占用，等待后重试... (错误: {ioEx.Message})");
                    Thread.Sleep(500 * (i + 1));
                }

            if (replaced)
            {
                // Linux: 确保新文件有执行权限
                if (IsLinux)
                {
                    var chmodProcess = Process.Start("chmod", $"+x \"{oldPath}\"");
                    chmodProcess?.WaitForExit();
                }
                
                // 关键步骤4：启动更新后的程序
                var finalStartInfo = new ProcessStartInfo
                {
                    FileName = oldPath,
                    UseShellExecute = !IsLinux,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                
                // Linux 特殊处理
                if (IsLinux)
                {
                    finalStartInfo.UseShellExecute = false;
                    finalStartInfo.CreateNoWindow = true;
                }

                Console.WriteLine($@"启动更新后的程序: {oldPath}");
                Process.Start(finalStartInfo);
                Console.WriteLine(@"更新流程完成");
            }
            else
            {
                Console.WriteLine(@"文件替换失败，可能被其他进程锁定");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"替换过程失败: {ex.Message}", ex);
        }
        finally
        {
            Environment.Exit(0);
        }
    }
}