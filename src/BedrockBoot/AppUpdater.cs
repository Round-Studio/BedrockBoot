using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace BedrockBoot;

/// <summary>
///     应用程序更新器
/// </summary>
public static class AppUpdater
{
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
                        var targetExePath = args[i + 1];
                        LaunchUpdateReplacement(targetExePath);
                        Environment.Exit(0); // 引导程序使命完成，退出
                    }

                    break;
                case "--update-replace":
                    // 模式2：作为替换程序启动
                    if (i + 1 < args.Length)
                    {
                        var oldExePath = args[i + 1];
                        PerformFileReplacement(oldExePath);
                        // 替换完成后，此进程会自动退出
                        Environment.Exit(0);
                    }

                    break;
            }
        // 没有更新参数，正常继续启动Avalonia应用
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
            // 使用 --update-launcher 参数，并传递最终需要被替换的目标文件路径
            var launcherInfo = new ProcessStartInfo
            {
                FileName = currentExePath,
                Arguments = $"--update-launcher \"{oldVersionFullPath}\"",
                UseShellExecute = true, // 启动新窗口
                WindowStyle = ProcessWindowStyle.Normal
            };

            Console.WriteLine($@"启动更新引导程序: {currentExePath}");
            Process.Start(launcherInfo);

            // 当前旧版本程序可以在这里退出，让新引导程序接管
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
    private static void LaunchUpdateReplacement(string targetExePath)
    {
        try
        {
            var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
            var tempDir = Path.GetTempPath();
            var tempExeName = $"BedrockBoot_Update_{Guid.NewGuid():N}.exe";
            var tempExePath = Path.Combine(tempDir, tempExeName);

            Console.WriteLine($@"引导程序：复制到临时位置 {tempExePath}");

            // 复制当前程序到临时位置
            File.Copy(currentExePath, tempExePath, true);

            // 关键步骤2：启动临时副本作为替换程序
            // 使用 --update-replace 参数，并传递需要被替换的原始文件路径
            var replaceInfo = new ProcessStartInfo
            {
                FileName = tempExePath,
                Arguments = $"--update-replace \"{targetExePath}\"",
                UseShellExecute = false, // 不依赖Shell，更可靠
                CreateNoWindow = true // 静默执行替换
            };

            Console.WriteLine($@"启动替换程序来更新 {targetExePath}");
            Process.Start(replaceInfo);

            // 引导程序完成任务，退出
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
    private static void PerformFileReplacement(string oldExePath)
    {
        try
        {
            var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;

            Console.WriteLine($@"替换程序：准备替换 {oldExePath}");

            // 确保原进程已退出，重试多次
            var replaced = false;
            for (var i = 0; i < 5; i++) // 最多重试5次
                try
                {
                    // 关键步骤3：执行文件替换
                    File.Delete(oldExePath); // 删除旧文件
                    File.Move(currentExePath, oldExePath); // 移动新文件到目标位置
                    replaced = true;
                    Console.WriteLine($@"文件替换成功 (第{i + 1}次尝试)");
                    break;
                }
                catch (IOException ioEx) when (i < 4) // 前4次失败重试
                {
                    Console.WriteLine($@"文件被占用，等待后重试... (错误: {ioEx.Message})");
                    Thread.Sleep(500 * (i + 1)); // 递增等待
                }

            if (replaced)
            {
                // 关键步骤4：启动更新后的程序
                var finalStartInfo = new ProcessStartInfo
                {
                    FileName = oldExePath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Console.WriteLine($@"启动更新后的程序: {oldExePath}");
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
            // 替换程序使命完成，无论成功失败都退出
            Environment.Exit(0);
        }
    }
}