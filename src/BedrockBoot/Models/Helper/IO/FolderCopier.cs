using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Helper.IO;

public class FolderCopier
{
    /// <summary>
    /// 异步复制文件夹
    /// </summary>
    /// <param name="sourceFolder">源文件夹路径</param>
    /// <param name="destinationFolder">目标文件夹路径</param>
    /// <param name="progressCallback">进度回调 (当前文件索引, 总文件数, 当前文件路径, 已复制字节数, 总字节数)</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="copySubDirectories">是否复制子目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task CopyAsync(
        string sourceFolder,
        string destinationFolder,
        IProgress<(int currentFile, int totalFiles, string fileName, long copiedBytes, long totalBytes)> progressCallback = null,
        bool overwrite = false,
        bool copySubDirectories = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sourceFolder))
            throw new ArgumentException("源文件夹路径不能为空", nameof(sourceFolder));
        if (string.IsNullOrEmpty(destinationFolder))
            throw new ArgumentException("目标文件夹路径不能为空", nameof(destinationFolder));
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");
        
        // 获取所有需要复制的文件
        var files = Directory.GetFiles(sourceFolder, "*", copySubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        int totalFiles = files.Length;
        long totalBytes = files.Sum(f => new FileInfo(f).Length);
        long copiedBytes = 0;
        
        int currentFileIndex = 0;
        
        foreach (var sourceFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 计算相对路径
            var relativePath = GetRelativePath(sourceFolder, sourceFile);
            var destFile = Path.Combine(destinationFolder, relativePath);
            
            // 确保目标目录存在
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            
            // 复制文件
            var fileInfo = new FileInfo(sourceFile);
            var fileSize = fileInfo.Length;
            
            await CopyFileAsync(sourceFile, destFile, overwrite, cancellationToken);
            
            copiedBytes += fileSize;
            currentFileIndex++;
            
            // 报告进度
            progressCallback?.Report((
                currentFileIndex, 
                totalFiles, 
                relativePath, 
                copiedBytes, 
                totalBytes
            ));
        }
    }
    
    /// <summary>
    /// 同步复制文件夹
    /// </summary>
    /// <param name="sourceFolder">源文件夹路径</param>
    /// <param name="destinationFolder">目标文件夹路径</param>
    /// <param name="progressCallback">进度回调 (当前文件索引, 总文件数, 当前文件路径, 已复制字节数, 总字节数)</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="copySubDirectories">是否复制子目录</param>
    public static void Copy(
        string sourceFolder,
        string destinationFolder,
        Action<int, int, string, long, long> progressCallback = null,
        bool overwrite = false,
        bool copySubDirectories = true)
    {
        if (string.IsNullOrEmpty(sourceFolder))
            throw new ArgumentException("源文件夹路径不能为空", nameof(sourceFolder));
        if (string.IsNullOrEmpty(destinationFolder))
            throw new ArgumentException("目标文件夹路径不能为空", nameof(destinationFolder));
        if (!Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException($"源文件夹不存在: {sourceFolder}");
        
        var files = Directory.GetFiles(sourceFolder, "*", copySubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        int totalFiles = files.Length;
        long totalBytes = files.Sum(f => new FileInfo(f).Length);
        long copiedBytes = 0;
        
        int currentFileIndex = 0;
        
        foreach (var sourceFile in files)
        {
            var relativePath = GetRelativePath(sourceFolder, sourceFile);
            var destFile = Path.Combine(destinationFolder, relativePath);
            
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            
            var fileInfo = new FileInfo(sourceFile);
            var fileSize = fileInfo.Length;
            
            File.Copy(sourceFile, destFile, overwrite);
            
            copiedBytes += fileSize;
            currentFileIndex++;
            
            progressCallback?.Invoke(currentFileIndex, totalFiles, relativePath, copiedBytes, totalBytes);
        }
    }
    
    private static async Task CopyFileAsync(string sourcePath, string destPath, bool overwrite, CancellationToken cancellationToken)
    {
        const int bufferSize = 81920;
        
        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
        using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
        {
            await sourceStream.CopyToAsync(destStream, bufferSize, cancellationToken);
            await destStream.FlushAsync(cancellationToken);
        }
    }
    
    private static string GetRelativePath(string basePath, string fullPath)
    {
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            basePath += Path.DirectorySeparatorChar;
        
        var relativePath = fullPath.Substring(basePath.Length);
        return relativePath;
    }
}