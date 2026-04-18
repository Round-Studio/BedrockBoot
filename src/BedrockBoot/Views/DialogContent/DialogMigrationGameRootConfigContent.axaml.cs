using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using SearchOption = System.IO.SearchOption;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMigrationGameRootConfigContent : UserControl
{
    public DialogMigrationGameRootConfigContent()
    {
        InitializeComponent();
    }

    public DialogMigrationGameRootConfigContent(VersionConfig versionInfo) : this()
    {
        VersionInfo = versionInfo;
        Migration();
    }

    public VersionConfig VersionInfo { get; set; }

    public void Migration()
    {
        Task.Run(async () =>
        {
            var sourceRoot = IsolationCore.GetInstanceConfigRootPath(VersionInfo);
            if (!Directory.Exists(sourceRoot))
            {
                Dispatcher.UIThread.Invoke(DialogHost.Close);
                Dispatcher.UIThread.Invoke(() => TaskLaunchGameItem.Launch(VersionInfo));
                return;
            }

            // 获取目标隔离根目录（实例专属）
            var targetIsolationRoot = IsolationCore.GetRealPath(VersionInfo);

            try
            {
                Console.WriteLine($@"即将迁移源目录：{sourceRoot}");
                Console.WriteLine($@"目标隔离目录：{targetIsolationRoot}");

                // 确定源数据中的 "com.mojang" 路径
                string? mojangSourcePath = null;

                if (VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.UWP)
                    mojangSourcePath = Path.Combine(sourceRoot, "LocalState", "games", "com.mojang");
                else if (VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.GDK)
                    mojangSourcePath = Path.Combine(sourceRoot, "games", "com.mojang");

                if (string.IsNullOrEmpty(mojangSourcePath) || !Directory.Exists(mojangSourcePath))
                {
                    Console.WriteLine(@"未找到 com.mojang 数据目录，跳过迁移。");
                    goto SKIP_MIGRATION;
                }

                // 确定目标 com.mojang 路径（根据 BuildType）
                string mojangTargetPath;
                if (VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.UWP)
                    mojangTargetPath = Path.Combine(targetIsolationRoot, "LocalState", "games", "com.mojang");
                else
                    mojangTargetPath = Path.Combine(targetIsolationRoot, "Users", "Shared", "games", "com.mojang");

                Directory.CreateDirectory(Path.GetDirectoryName(mojangTargetPath)!);

                Console.WriteLine($@"迁移 {mojangSourcePath} → {mojangTargetPath}");

                var files = Directory.GetFiles(mojangSourcePath, "*", SearchOption.AllDirectories);
                var totalFiles = files.Length;
                Console.WriteLine($@"文件总数：{totalFiles}");

                if (totalFiles > 0)
                {
                    // 创建进度报告器
                    var progress = new Progress<(int current, int total, string fileName)>(report =>
                    {
                        var (current, total, fileName) = report;
                        var percentage = total > 0 ? (double)current / total * 100 : 0;

                        // 假设你在 XAML 中有一个 ProgressBar 控件叫 ProgressIndicator
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            // 示例：更新进度条（请替换为你实际的 UI 控件）
                            // ProgressIndicator.Value = percentage;
                            // ProgressLabel.Text = $"正在迁移: {fileName} ({current}/{total})";
                        });
                    });

                    await CopyDirectoryWithProgressAsync(mojangSourcePath, mojangTargetPath, progress, totalFiles);
                }

                await Task.Delay(500); // 等待句柄释放

                SKIP_MIGRATION: ;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"迁移过程中发生错误：{ex.Message}");
            }
            finally
            {
                // 清理完成后关闭对话框并启动游戏
                DeleteDirectorySafe(sourceRoot);

                Dispatcher.UIThread.Invoke(DialogHost.Close);
                Dispatcher.UIThread.Invoke(() => TaskLaunchGameItem.Launch(VersionInfo));
            }
        });
    }

    // 新增：带进度的异步复制
    private static async Task CopyDirectoryWithProgressAsync(
        string sourceDir,
        string destinationDir,
        IProgress<(int current, int total, string fileName)> progress,
        int totalFiles)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");

        Directory.CreateDirectory(destinationDir);

        var files = dir.GetFiles("*", SearchOption.AllDirectories);
        var copiedCount = 0;

        // 复制所有文件
        foreach (var file in files)
            try
            {
                var relativePath = Path.GetRelativePath(sourceDir, file.FullName);
                var destPath = Path.Combine(destinationDir, relativePath);
                var destDirPath = Path.GetDirectoryName(destPath)!;

                Directory.CreateDirectory(destDirPath);
                file.CopyTo(destPath, true);

                copiedCount++;
                progress?.Report((copiedCount, totalFiles, Path.GetFileName(file.Name)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"复制文件 {file.Name} 时出错: {ex.Message}");
            }

        // 如果需要复制子目录（递归），可以在这里添加逻辑
        // 但当前只需复制文件即可
    }

    // 更安全的删除目录方法
    private static void DeleteDirectorySafe(string path, int maxRetries = 3)
    {
        if (!Directory.Exists(path))
            return;

        for (var retry = 0; retry < maxRetries; retry++)
            try
            {
                if (!Directory.Exists(path)) return;
                Directory.Delete(path, true);
                return;
            }
            catch (IOException ex) when (retry < maxRetries - 1)
            {
                Console.WriteLine($@"删除目录时出错 (尝试 {retry + 1}/{maxRetries}): {ex.Message}");

                if (ex.Message.Contains("被另一个进程使用") || ex.Message.Contains("正在使用"))
                {
                    Task.Delay(1000).Wait();
                }
                else
                {
                    DeleteDirectoryContentsManually(path);
                    Task.Delay(500).Wait();
                }
            }
            catch (UnauthorizedAccessException ex) when (retry < maxRetries - 1)
            {
                Console.WriteLine($@"权限错误 (尝试 {retry + 1}/{maxRetries}): {ex.Message}");
                ResetFileAttributes(path);
                Task.Delay(500).Wait();
            }

        Console.WriteLine($@"无法删除目录: {path}");
    }

    private static void DeleteDirectoryContentsManually(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // 忽略
                }

            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length);
            foreach (var dir in dirs)
                try
                {
                    Directory.Delete(dir, false);
                }
                catch
                {
                    // 忽略
                }

            Directory.Delete(path, false);
        }
        catch
        {
            // 忽略
        }
    }

    private static void ResetFileAttributes(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch
                {
                    // 忽略
                }
        }
        catch
        {
            // 忽略
        }
    }
}