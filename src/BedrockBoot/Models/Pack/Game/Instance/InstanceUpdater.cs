using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum.Type.Progress.Steps;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Helper;
using BedrockBoot.Services;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Models.Pack.Game.Instance;

public class InstanceUpdater
{
    private readonly VersionConfig _versionConfig;
    private bool _isOldDelete = false;
    public IProgress<InstanceUpdateProgress>? Progress { get; set; }
    public required Func<List<GameDownloadUrlInfo>, string> ChooseDownloadUrl { get; set; }

    public InstanceUpdater(VersionConfig versionConfig)
    {
        _versionConfig = versionConfig;
    }

    public List<BuildInfo> GetUpdateableVersions()
    {
        var currentVersion = Version.Parse(_versionConfig.Info.Version);
        var allVersions = VersionHelper.GetVersions();

        return allVersions
            .Where(buildInfo => Version.Parse(buildInfo.ID) > currentVersion)
            .Where(info => info.BuildType == _versionConfig.Info.BuildType)
            .OrderBy(info => Version.Parse(info.ID))
            .ToList();
    }

    public async Task UpdateAsync(BuildInfo buildInfo)
    {
        Console.WriteLine($@"开始升级实例：{_versionConfig.VersionPath} 版本：{_versionConfig.Info.Version} -> {buildInfo.ID}");
        
        var downloader = new EasyDownload(buildInfo, true, _versionConfig.VersionsRootPath,
            Path.GetFileName(_versionConfig.VersionPath), true);

        string protectedPath = Path.GetFullPath(Path.Combine(_versionConfig.VersionPath, "config", "BedrockBoot2"));

        var deleteTask = Task.Run(() =>
        {
            Console.WriteLine(@"开始卸载旧版本内容");

            Progress?.Report(new()
            {
                Message = "删除旧版本文件",
                Progress = 0,
                Step = InstanceUpdateStep.DeleteOld
            });

            var runDirectory = _versionConfig.VersionPath;
            var rootDirInfo = new DirectoryInfo(runDirectory);
            var filesToDelete = new List<string>();
            var dirsToDelete = new List<string>();

            CollectFilesAndDirectoriesToDelete(rootDirInfo, protectedPath, filesToDelete, dirsToDelete);

            int totalCount = filesToDelete.Count + dirsToDelete.Count;
            int deletedCount = 0;

            foreach (var filePath in filesToDelete)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"删除文件失败: {filePath}, 错误: {ex.Message}");
                }
                finally
                {
                    deletedCount++;
                    int progressPercent = totalCount > 0 ? (int)((double)deletedCount / totalCount * 100) : 100;
                    Progress?.Report(new()
                    {
                        Message = $"正在删除文件 ({deletedCount}/{totalCount})",
                        Progress = progressPercent,
                        Step = InstanceUpdateStep.DeleteOld
                    });
                }
            }

            foreach (var dirPath in dirsToDelete.OrderByDescending(d => d.Length))
            {
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"删除目录失败: {dirPath}, 错误: {ex.Message}");
                }
                finally
                {
                    deletedCount++;
                    int progressPercent = totalCount > 0 ? (int)((double)deletedCount / totalCount * 100) : 100;
                    Progress?.Report(new()
                    {
                        Message = $"正在删除目录 ({deletedCount}/{totalCount})",
                        Progress = progressPercent,
                        Step = InstanceUpdateStep.DeleteOld
                    });
                }
            }

            Progress?.Report(new()
            {
                Message = "旧版本文件删除完成",
                Progress = 100,
                Step = InstanceUpdateStep.DeleteOld
            });

            downloader.IsCanInstall = true;
        });

        var installTask = Task.Run(async () =>
        {
            downloader.DownloadProgress = (message, progressInfo) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.Download,
                    Progress = progressInfo.Percentage,
                    Message = message,
                    Detailed = $"速度: {progressInfo.Speed}"
                });
            };

            downloader.MergeProgress = (message, progress) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.Download,
                    Progress = progress,
                    Message = message,
                    Detailed = "合并文件中..."
                });
            };

            downloader.ExtractionProgress = (message, progress) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.UnZip,
                    Progress = progress,
                    Message = message,
                    Detailed = "解压文件中..."
                });
            };

            downloader.DeploymentProgress = (message, progress) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.UWPRegistering,
                    Progress = progress.percentage,
                    Message = message,
                    Detailed = "注册应用中..."
                });
            };

            downloader.Completed = (config) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.UpdateFinish,
                    Progress = 100,
                    Message = "更新完成",
                    Detailed = ""
                });
            };

            downloader.ErrorOccurred = (title, message, ex) =>
            {
                Progress?.Report(new()
                {
                    Step = InstanceUpdateStep.Download,
                    Progress = 100,
                    Message = $"{title}: {message}",
                    Detailed = ex?.Message
                });
            };

            var urls = await EasyDownload.GetPackageUrls(buildInfo);
            var url = ChooseDownloadUrl.Invoke(urls);
            await downloader.InstallAsync(url, default, true);
        });

        await Task.WhenAll(deleteTask, installTask);

        _versionConfig.Info.BuildType = buildInfo.BuildType;
        _versionConfig.Info.Version = buildInfo.ID;
        _versionConfig.Info.VersionType = buildInfo.Type;
        GameInfoHelper.SaveVersionConfig(_versionConfig);

        Console.WriteLine(@"更新完成");
    }

    private void CollectFilesAndDirectoriesToDelete(DirectoryInfo directory, string protectedPath, List<string> filesToDelete, List<string> dirsToDelete)
    {
        try
        {
            string currentDirPath = NormalizePath(directory.FullName);
            string formattedProtectedPath = NormalizePath(protectedPath);

            if (IsSameOrSubPath(currentDirPath, formattedProtectedPath))
            {
                return;
            }

            bool containsProtectedChild = false;
            foreach (var subDir in directory.GetDirectories())
            {
                string subDirPath = NormalizePath(subDir.FullName);
                if (IsSameOrSubPath(subDirPath, formattedProtectedPath))
                {
                    containsProtectedChild = true;
                    break;
                }
            }

            foreach (var file in directory.GetFiles())
            {
                string filePath = NormalizePath(file.FullName);
                if (!IsSameOrSubPath(filePath, formattedProtectedPath))
                {
                    filesToDelete.Add(file.FullName);
                }
            }

            foreach (var subDir in directory.GetDirectories())
            {
                CollectFilesAndDirectoriesToDelete(subDir, protectedPath, filesToDelete, dirsToDelete);
            }

            if (!containsProtectedChild && !IsSameOrSubPath(currentDirPath, formattedProtectedPath))
            {
                bool hasFiles = Directory.GetFiles(directory.FullName).Any();
                bool hasSubDirs = Directory.GetDirectories(directory.FullName).Any();
                
                if (!hasFiles && !hasSubDirs)
                {
                    dirsToDelete.Add(directory.FullName);
                }
                else
                {
                    bool allSubDirsAreProtected = true;
                    foreach (var subDir in directory.GetDirectories())
                    {
                        string subDirPath = NormalizePath(subDir.FullName);
                        if (!IsSameOrSubPath(subDirPath, formattedProtectedPath))
                        {
                            allSubDirsAreProtected = false;
                            break;
                        }
                    }
                    
                    bool allFilesInSubDirs = true;
                    if (directory.GetFiles().Any())
                    {
                        allFilesInSubDirs = false;
                    }
                    
                    if (allSubDirsAreProtected && allFilesInSubDirs)
                    {
                        dirsToDelete.Add(directory.FullName);
                    }
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($@"无法访问目录: {directory.FullName}, 错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"收集删除项时出错: {directory.FullName}, 错误: {ex.Message}");
        }
    }

    private string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private bool IsSameOrSubPath(string path, string protectedPath)
    {
        return path.Equals(protectedPath, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(protectedPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}