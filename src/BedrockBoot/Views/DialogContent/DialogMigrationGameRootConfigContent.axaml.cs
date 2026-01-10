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
        Task.Run(() =>
        {
            var path = IsolationCore.GetInstanceConfigRootPath(VersionInfo);
            Console.WriteLine($"即将迁移文件夹：{path}");
        
            var files = Directory.GetFiles(path,"*", SearchOption.AllDirectories);
            Console.WriteLine($"总数目：{files.Length}");

            CopyDirectory(path, PathsList.GamePublicRootPath, true);
            
            Directory.Delete(path, true);

            Dispatcher.UIThread.Invoke(DialogHost.Close);
            Dispatcher.UIThread.Invoke(() => TaskLaunchGameItem.Launch(VersionInfo));
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
            string tempPath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(tempPath, true); // false 表示不覆盖已存在的文件
        }

        // 如果要复制子目录，则递归复制
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destinationDir, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }
}