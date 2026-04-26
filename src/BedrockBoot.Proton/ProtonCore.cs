using System.Diagnostics;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Proton.Entry.Config;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Proton.Enum;
using Round.SDK.Entity;
using SourceList = BedrockBoot.Proton.Global.SourceList;

namespace BedrockBoot.Proton;

public class ProtonCore
{
    public static ConfigEntity<ProtonConfig> Config { get; set; } = new(PathsList.ProtonConfigPath);
    public static void InitializeEnvironment()
    {
        if (OperatingSystem.IsWindows())
            return;
        
        var dir = PathsList.ProtonPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        
        Config.Load();
    }
    public static async Task<IReadOnlyList<ProtonInfo>?> GetInstallableVersion(ProtonSource sourceType)
    {
        if (OperatingSystem.IsWindows())
            return null;
        
        var releases = await SourceList.ProtonRepository[sourceType];
        var result = releases.Select(release => new ProtonInfo()
        {
            Name = release.Name,
            Version = release.Name,
            ReleaseUrl = release.Assets
                .Where(asset => asset.Name.Contains("Proton", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.BrowserDownloadUrl)
                .FirstOrDefault() ?? string.Empty,
            ReleaseSize = release.Assets
                .Where(asset => asset.Name.Contains("Proton", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.Size)
                .FirstOrDefault(),
            Branch = sourceType
        });

        return result.ToList();
    }
    public static async Task<string> InstallProton(ProtonInfo info, InstallInfo installInfo,
        IProgress<DownloadProgress> progress = null,bool isDefaultProton = false)
    {
        progress?.Report(new()
        {
            Message = "准备中..."
        });

        var url = info.ReleaseUrl;
        var filePath = Path.Combine(PathsList.ProtonPath, "download", Path.GetFileName(url));
        var installPath = Path.Combine(PathsList.ProtonPath, "bin", installInfo.InstallName);

        if (Directory.Exists(installPath))
        {
            if (!installInfo.IsOverWrite)
                throw new FileNotFoundException("目标版本以安装");
            else
                Directory.Delete(installPath, true);
        }

        if (!(File.Exists(filePath) &&
              new FileInfo(filePath).Length == info.ReleaseSize))
        {
            var downloader = new GithubFilesDownloader();
            await downloader.DownloadAsync(url, filePath,
                new Progress<DownloadProgress>(p => progress?.Report(new DownloadProgress()
                {
                    BytesPerSecond = p.BytesPerSecond,
                    DownloadedBytes = p.DownloadedBytes,
                    EstimatedRemainingSeconds = p.EstimatedRemainingSeconds,
                    Message = "下载文件",
                    TotalBytes = p.TotalBytes
                })));
        }

        var unZipPath = Path.Combine(PathsList.TempPath,
            $"{Path.GetFileName(url)}_{Guid.NewGuid().ToString("N")}");

        if (!Directory.Exists(unZipPath))
            Directory.CreateDirectory(unZipPath);

        UnZip(filePath, unZipPath);

        var rootPath = unZipPath;
        if (Directory.GetDirectories(rootPath).Length == 1)
            rootPath = Directory.GetDirectories(rootPath).First();

        Console.WriteLine("Root Path: " + rootPath);

        Directory.CreateDirectory(installPath);
        await CopyFilesWithProgressAsync(rootPath, installPath, progress);

        var conf = new ConfigEntity<ProtonInfo>(Path.Combine(installPath, ".bb", "version.json"));
        conf.Data = info;
        conf.Data.Name = installInfo.InstallName;
        conf.Data.IsDefault = isDefaultProton;
        conf.Save();

        return installPath;
    }
    public static IReadOnlyList<ProtonInfo>? GetInstalledVersions()
    {
        if (!Directory.Exists(Path.Combine(PathsList.ProtonPath, "bin"))) return null;
        
        var dirs = Directory.GetDirectories(Path.Combine(PathsList.ProtonPath, "bin"));
        return dirs.ToList().Select(dir =>
        {
            var conf = new ConfigEntity<ProtonInfo>(Path.Combine(dir, ".bb", "version.json"), false);
            conf.Load();
            conf.Data.InstallPath = dir;

            return conf.Data;
        }).ToList();
    }
    
    private static async Task CopyFilesWithProgressAsync(string sourcePath, string destPath, IProgress<DownloadProgress> progress = null)
    {
        Directory.CreateDirectory(destPath);
    
        // 获取所有文件
        var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
        var totalFiles = allFiles.Length;
    
        if (allFiles.Length == 0)
        {
            progress?.Report(new DownloadProgress { Message = "没有找到需要复制的文件", TotalBytes = totalFiles });
            return;
        }
    
        var sourceBasePath = sourcePath.TrimEnd(Path.DirectorySeparatorChar);
        var processedFiles = 0;
    
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(sourceBasePath, file);
            var targetFile = Path.Combine(destPath, relativePath);
        
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);
        
            await Task.Run(() => File.Copy(file, targetFile, true));
            
            processedFiles++;
        
            progress?.Report(new DownloadProgress
            {
                Message = $"复制文件",
                TotalBytes = totalFiles,
                DownloadedBytes = processedFiles
            });
        }

        progress?.Report(new DownloadProgress
        {
            Message = "文件复制完成", 
            TotalBytes = totalFiles,
            DownloadedBytes = processedFiles
        });
    }
    private static void UnZip(string zipPath, string unZipPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "tar",
            RedirectStandardError = false, // 重定向错误流以便排查
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-xzf");
        startInfo.ArgumentList.Add(zipPath);
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(unZipPath);

        using var process = Process.Start(startInfo);
        
        if (process == null)
            throw new Exception("无法启动 tar 进程。");

        process.WaitForExit();
    }
}