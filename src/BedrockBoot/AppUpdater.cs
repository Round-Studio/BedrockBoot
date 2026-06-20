using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Global;

namespace BedrockBoot;

public static class AppUpdater
{
    private const int ReplacementRetryCount = 8;
    private const int RetryDelayMilliseconds = 500;

    private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    ///     在程序启动初期处理更新专用参数。
    ///     支持新版参数 (--update-launcher, --update-replace) 和旧版参数 (-update)
    /// </summary>
    public static void ProcessStartupArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--update-launcher":
                    if (TryGetArgument(args, i + 1, out var targetPath) &&
                        TryGetArgument(args, i + 2, out var sourcePath))
                    {
                        LaunchUpdateReplacement(targetPath, sourcePath);
                        Environment.Exit(0);
                    }
                    break;
                    
                case "--update-replace":
                    if (TryGetArgument(args, i + 1, out var replacementTargetPath) &&
                        TryGetArgument(args, i + 2, out var replacementSourcePath))
                    {
                        PerformFileReplacement(replacementTargetPath, replacementSourcePath);
                        Environment.Exit(0);
                    }
                    break;
                
                // 新版更新参数
                case "-updatev2":
                    if (TryGetArgument(args, i + 1, out var downloadedFilePath))
                    {
                        Console.WriteLine($@"[AppUpdater] 检测到新版更新调用 (v2)，使用新文件: {downloadedFilePath}");
                        StartUpdateFromDownloadedFile(downloadedFilePath);
                        Environment.Exit(0);
                    }
                    break;
                
                // 兼容旧版更新参数 - 仅用于旧版程序调用，新版程序自身不会使用
                case "-update":
                    if (TryGetArgument(args, i + 1, out var oldVersionPath))
                    {
                        Console.WriteLine($@"[AppUpdater] 检测到旧版更新调用 (legacy)，待更新旧版: {oldVersionPath}");
                        StartUpdateFromOldVersion(oldVersionPath);
                        Environment.Exit(0);
                    }
                    break;
            }
    }

    /// <summary>
    ///     首选更新入口。
    ///     当前进程是"已安装程序"，参数是"已下载完成的新版本文件路径"。
    /// </summary>
    public static void StartUpdateFromDownloadedFile(string downloadedPayloadPath)
    {
        try
        {
            var installedExecutablePath = GetCurrentExecutablePath();
            if (string.IsNullOrWhiteSpace(installedExecutablePath))
            {
                Console.WriteLine(@"[AppUpdater] 无法获取当前安装程序路径");
                return;
            }

            // 确保下载的文件存在
            if (!File.Exists(downloadedPayloadPath))
            {
                Console.WriteLine($@"[AppUpdater] 下载的更新文件不存在: {downloadedPayloadPath}");
                return;
            }

            StartUpdateLauncher(installedExecutablePath, downloadedPayloadPath, installedExecutablePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 启动更新流程失败: {ex}");
        }
    }

    /// <summary>
    ///     兼容旧版本的更新入口（保留作为备用）
    /// </summary>
    public static void StartUpdateFromOldVersion(string installedExecutablePath)
    {
        try
        {
            var downloadedPayloadPath = GetCurrentExecutablePath();
            if (string.IsNullOrWhiteSpace(downloadedPayloadPath))
            {
                Console.WriteLine(@"[AppUpdater] 无法获取下载后的更新文件路径");
                return;
            }

            StartUpdateLauncher(installedExecutablePath, downloadedPayloadPath, downloadedPayloadPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 启动兼容更新流程失败: {ex}");
        }
    }

    /// <summary>
    ///     启动一个轻量 helper 实例，再由 helper 复制自己为 runner。
    /// </summary>
    private static void StartUpdateLauncher(
        string targetPath,
        string sourcePath,
        string launcherExecutablePath)
    {
        var resolvedTargetPath = NormalizePath(targetPath);
        var resolvedSourcePath = NormalizePath(sourcePath);
        var resolvedLauncherExecutablePath = NormalizePath(launcherExecutablePath);

        Console.WriteLine($@"[AppUpdater] 开始更新流程，目标文件: {resolvedTargetPath}");
        Console.WriteLine($@"[AppUpdater] 更新源文件: {resolvedSourcePath}");

        if (!File.Exists(resolvedTargetPath))
        {
            Console.WriteLine(@"[AppUpdater] 目标程序不存在，无法更新");
            return;
        }

        if (!File.Exists(resolvedSourcePath))
        {
            Console.WriteLine(@"[AppUpdater] 更新源文件不存在，无法更新");
            return;
        }

        if (resolvedTargetPath.Equals(resolvedSourcePath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(@"[AppUpdater] 更新源文件与目标文件路径相同，跳过更新");
            return;
        }

        var launcherInfo = new ProcessStartInfo
        {
            FileName = resolvedLauncherExecutablePath,
            Arguments = $"--update-launcher \"{resolvedTargetPath}\" \"{resolvedSourcePath}\"",
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(resolvedLauncherExecutablePath) ?? Environment.CurrentDirectory
        };

        Console.WriteLine($@"[AppUpdater] 启动更新引导程序: {resolvedLauncherExecutablePath}");
        Process.Start(launcherInfo);

        Console.WriteLine(@"[AppUpdater] 更新引导程序已启动，当前进程即将退出");
        Environment.Exit(0);
    }

    /// <summary>
    ///     由 helper 实例复制出 runner，再由 runner 执行真正的替换。
    /// </summary>
    private static void LaunchUpdateReplacement(string targetPath, string sourcePath)
    {
        try
        {
            var currentPath = GetCurrentExecutablePath();
            if (string.IsNullOrWhiteSpace(currentPath) || !File.Exists(currentPath))
            {
                Console.WriteLine(@"[AppUpdater] 无法获取当前更新程序路径");
                return;
            }

            var resolvedTargetPath = NormalizePath(targetPath);
            var resolvedSourcePath = NormalizePath(sourcePath);
            
            if (!File.Exists(resolvedTargetPath) || !File.Exists(resolvedSourcePath))
            {
                Console.WriteLine(@"[AppUpdater] 目标文件或更新源文件不存在，无法继续更新");
                return;
            }

            var updateWorkspace = PrepareUpdateWorkspace();
            CleanupUpdaterWorkspace(updateWorkspace);

            var runnerPath = Path.Combine(
                updateWorkspace,
                $"updater_runner_{Environment.ProcessId}_{Guid.NewGuid():N}{GetExecutableSuffix(currentPath)}");

            Console.WriteLine($@"[AppUpdater] 引导程序：复制 runner 到 {runnerPath}");
            File.Copy(currentPath, runnerPath, true);
            EnsureExecutableBit(runnerPath);

            var replaceInfo = new ProcessStartInfo
            {
                FileName = runnerPath,
                Arguments = $"--update-replace \"{resolvedTargetPath}\" \"{resolvedSourcePath}\"",
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(runnerPath) ?? Environment.CurrentDirectory
            };

            Console.WriteLine($@"[AppUpdater] 启动替换程序来更新 {resolvedTargetPath}");
            Process.Start(replaceInfo);

            Console.WriteLine(@"[AppUpdater] 更新引导程序退出");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 引导更新失败: {ex}");
        }
    }

    /// <summary>
    ///     runner 执行文件替换操作
    /// </summary>
    private static void PerformFileReplacement(string targetPath, string sourcePath)
    {
        try
        {
            var resolvedTargetPath = NormalizePath(targetPath);
            var resolvedSourcePath = NormalizePath(sourcePath);

            if (!File.Exists(resolvedSourcePath))
            {
                Console.WriteLine(@"[AppUpdater] 替换程序无法定位更新源文件");
                return;
            }

            if (resolvedTargetPath.Equals(resolvedSourcePath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(@"[AppUpdater] 更新源文件与目标文件路径相同，跳过替换");
                return;
            }

            var targetDirectory = Path.GetDirectoryName(resolvedTargetPath);
            if (string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(targetDirectory))
            {
                Console.WriteLine(@"[AppUpdater] 目标目录不存在，无法执行替换");
                return;
            }

            var stagePath = Path.Combine(
                targetDirectory,
                $"{Path.GetFileName(resolvedTargetPath)}.update-stage");
            var backupPath = Path.Combine(
                targetDirectory,
                $"{Path.GetFileName(resolvedTargetPath)}.bak");

            Console.WriteLine($@"[AppUpdater] 替换程序：准备使用 {resolvedSourcePath} 更新 {resolvedTargetPath}");
            
            TryDeleteFile(stagePath);
            File.Copy(resolvedSourcePath, stagePath, true);
            EnsureExecutableBit(stagePath);

            var replaced = false;
            Exception? lastException = null;

            for (var i = 0; i < ReplacementRetryCount; i++)
                try
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    if (File.Exists(resolvedTargetPath))
                        File.Move(resolvedTargetPath, backupPath);

                    File.Move(stagePath, resolvedTargetPath);
                    EnsureExecutableBit(resolvedTargetPath);
                    replaced = true;
                    Console.WriteLine($@"[AppUpdater] 文件替换成功 (第{i + 1}次尝试)");
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lastException = ex;
                    RestoreBackupIfNeeded(resolvedTargetPath, backupPath);

                    if (i == ReplacementRetryCount - 1)
                        break;

                    Console.WriteLine($@"[AppUpdater] 文件暂时不可替换，等待后重试... (错误: {ex.Message})");
                    Thread.Sleep(RetryDelayMilliseconds * (i + 1));
                }

            if (!replaced)
            {
                lastException ??= new IOException("替换重试次数已耗尽");
                RestoreBackupIfNeeded(resolvedTargetPath, backupPath);
                TryDeleteFile(stagePath);
                Console.WriteLine(@"[AppUpdater] 文件替换失败，可能被其他进程锁定");
                Console.WriteLine($@"[AppUpdater] 最后一次错误: {lastException.Message}");
                return;
            }

            var finalStartInfo = new ProcessStartInfo
            {
                FileName = resolvedTargetPath,
                UseShellExecute = false,
                WorkingDirectory = targetDirectory
            };

            Console.WriteLine($@"[AppUpdater] 启动更新后的程序: {resolvedTargetPath}");
            Process.Start(finalStartInfo);

            TryDeleteFile(backupPath);
            TryDeleteFile(resolvedSourcePath);
            Console.WriteLine(@"[AppUpdater] 更新流程完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 替换过程失败: {ex}");
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    private static string PrepareUpdateWorkspace()
    {
        Directory.CreateDirectory(PathsList.UpdatePath);
        return PathsList.UpdatePath;
    }

    private static void CleanupUpdaterWorkspace(string workspacePath)
    {
        foreach (var pattern in new[] { "updater_runner_*", "*.update-stage" })
            foreach (var file in Directory.GetFiles(workspacePath, pattern))
                TryDeleteFile(file);
    }

    private static string GetCurrentExecutablePath()
    {
        if (IsLinux)
        {
            var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
            if (!string.IsNullOrWhiteSpace(appImagePath) && File.Exists(appImagePath))
                return NormalizePath(appImagePath);
        }

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
            return NormalizePath(processPath);

        var mainModulePath = Process.GetCurrentProcess().MainModule?.FileName;
        return string.IsNullOrWhiteSpace(mainModulePath) ? string.Empty : NormalizePath(mainModulePath);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static string GetExecutableSuffix(string executablePath)
    {
        var extension = Path.GetExtension(executablePath);
        if (!string.IsNullOrWhiteSpace(extension))
            return extension;

        return IsWindows ? ".exe" : string.Empty;
    }

    private static void EnsureExecutableBit(string filePath)
    {
        if (!IsLinux || string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            File.SetUnixFileMode(
                filePath,
                UnixFileMode.UserRead
                | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead
                | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead
                | UnixFileMode.OtherExecute);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 设置可执行权限失败 {filePath}: {ex.Message}");
        }
    }

    public static void EnsureExecutableForCurrentPlatform(string filePath)
    {
        EnsureExecutableBit(filePath);
    }

    private static void RestoreBackupIfNeeded(string targetPath, string backupPath)
    {
        if (File.Exists(targetPath) || !File.Exists(backupPath))
            return;

        File.Move(backupPath, targetPath);
    }

    private static void TryDeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"[AppUpdater] 清理文件失败 {filePath}: {ex.Message}");
        }
    }

    private static bool TryGetArgument(string[] args, int index, out string value)
    {
        if (index < args.Length && !string.IsNullOrWhiteSpace(args[index]))
        {
            value = args[index];
            return true;
        }

        value = string.Empty;
        return false;
    }
}