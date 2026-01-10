using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Views.TaskItem;
using Microsoft.VisualBasic.FileIO;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using SearchOption = System.IO.SearchOption;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogMigrationGameRootConfigContent : UserControl
{
    public VersionConfig VersionInfo { get; set; }
    public DialogMigrationGameRootConfigContent()
    {
        InitializeComponent();
    }
    public DialogMigrationGameRootConfigContent(VersionConfig versionInfo):this()
    {
        VersionInfo = versionInfo;

        Migration();
    }
    
    public void Migration()
    {
        Task.Run(async () =>
        {
            try
            {
                var path = IsolationCore.GetInstanceConfigRootPath(VersionInfo);
                Console.WriteLine($"即将迁移文件夹：{path}");
            
                var files = Directory.GetFiles(path,"*", SearchOption.AllDirectories);
                Console.WriteLine($"总数目：{files.Length}");

                CopyDirectory(path, PathsList.GamePublicRootPath, true);
                
                // 延迟一段时间确保文件句柄释放
                await Task.Delay(500);
                
                // 使用更安全的方式删除目录
                DeleteDirectorySafe(path);
                
                Dispatcher.UIThread.Invoke(DialogHost.Close);
                Dispatcher.UIThread.Invoke(() => TaskLaunchGameItem.Launch(VersionInfo));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"迁移过程中发生错误：{ex.Message}");
                // 可以选择在这里显示错误信息给用户
            }
        });
    }
    
    public static void CopyDirectory(string sourceDir, string destinationDir, bool copySubDirs = true)
    {
        // 获取源目录的信息
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
        }

        // 如果目标目录不存在，则创建它
        DirectoryInfo[] dirs = dir.GetDirectories();
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        // 复制所有文件
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            try
            {
                string tempPath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(tempPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"复制文件 {file.Name} 时出错: {ex.Message}");
                // 继续处理其他文件
            }
        }

        // 如果要复制子目录，则递归复制
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                try
                {
                    string tempPath = Path.Combine(destinationDir, subdir.Name);
                    CopyDirectory(subdir.FullName, tempPath, copySubDirs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"复制目录 {subdir.Name} 时出错: {ex.Message}");
                    // 继续处理其他目录
                }
            }
        }
    }
    
    // 更安全的删除目录方法
    private static void DeleteDirectorySafe(string path, int maxRetries = 3)
    {
        if (!Directory.Exists(path))
            return;
            
        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                // 先尝试标准删除
                Directory.Delete(path, true);
                return;
            }
            catch (IOException ex) when (retry < maxRetries - 1)
            {
                Console.WriteLine($"删除目录时出错 (尝试 {retry + 1}/{maxRetries}): {ex.Message}");
                
                // 如果文件被占用，等待后重试
                if (ex.Message.Contains("被另一个进程使用") || ex.Message.Contains("正在使用"))
                {
                    Task.Delay(1000).Wait();
                }
                else
                {
                    // 对于其他错误，尝试逐个删除文件和目录
                    DeleteDirectoryContentsManually(path);
                    Task.Delay(500).Wait();
                }
            }
            catch (UnauthorizedAccessException ex) when (retry < maxRetries - 1)
            {
                Console.WriteLine($"权限错误 (尝试 {retry + 1}/{maxRetries}): {ex.Message}");
                
                // 尝试重置文件属性
                ResetFileAttributes(path);
                Task.Delay(500).Wait();
            }
        }
        
        // 如果所有重试都失败，记录日志但不抛出异常
        Console.WriteLine($"无法删除目录: {path}");
    }
    
    // 手动删除目录内容
    private static void DeleteDirectoryContentsManually(string path)
    {
        try
        {
            // 删除所有文件
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch
                {
                    // 忽略无法删除的文件
                }
            }
            
            // 删除所有子目录（从最深开始）
            var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length);
            foreach (var dir in dirs)
            {
                try
                {
                    Directory.Delete(dir, false);
                }
                catch
                {
                    // 忽略无法删除的目录
                }
            }
            
            // 最后删除根目录
            Directory.Delete(path, false);
        }
        catch
        {
            // 忽略错误
        }
    }
    
    // 重置文件属性（解决只读文件问题）
    private static void ResetFileAttributes(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch
                {
                    // 忽略错误
                }
            }
        }
        catch
        {
            // 忽略错误
        }
    }
}