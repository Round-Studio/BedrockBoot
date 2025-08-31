namespace BedrockBoot.Integration.Classes;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

public class ZipCreatorWithProgress
{
    public void CreateZipFromFolderAsync(string sourceFolder, string zipPath, IProgress<(double, string)> progress)
    {
        if (!Directory.Exists(sourceFolder))
        {
            throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");
        }

        // 获取所有文件（包括子目录）
        var allFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
        int totalFiles = allFiles.Length;
        int processedFiles = 0;

        // 确保目标目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath));

        // 删除已存在的ZIP文件
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (string file in allFiles)
            {
                try
                {
                    // 计算相对路径
                    string relativePath = GetRelativePath(sourceFolder, file);

                    // 创建ZIP条目
                    zipArchive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);

                    processedFiles++;

                    // 报告进度
                    double percentage = (double)processedFiles / totalFiles * 100;
                    if (processedFiles % 10 == 0)
                    {
                        progress.Report((percentage, $"打包中: {Path.GetFileName(file)} ({processedFiles}/{totalFiles})"));
                    }
                }
                catch (Exception ex)
                {
                    progress.Report((processedFiles * 100.0 / totalFiles, $"错误: {ex.Message}"));
                }
            }
        }

        progress.Report((100, "打包完成!"));
    }

    private string GetRelativePath(string basePath, string fullPath)
    {
        basePath = basePath.TrimEnd(Path.DirectorySeparatorChar);
        return fullPath.Substring(basePath.Length + 1);
    }
}