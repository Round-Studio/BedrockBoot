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
using BedrockBoot.Downloader.Game;
using BedrockBoot.Downloader.Info.Game;
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

    public List<VersionBuildInfo> GetUpdateableVersions()
    {
        var currentVersion = Version.Parse(_versionConfig.Info.Version);
        var allVersions = VersionHelper.GetVersionBuildInfoList();

        return allVersions
            .Where(buildInfo => Version.Parse(buildInfo.Id) > currentVersion)
            .Where(info => info.GameBuildType == _versionConfig.Info.BuildType)
            .OrderBy(info => Version.Parse(info.Id))
            .ToList();
    }

    public async Task UpdateAsync(VersionBuildInfo buildInfo)
    {
        Console.WriteLine($@"开始升级实例：{_versionConfig.VersionPath} 版本：{_versionConfig.Info.Version} -> {buildInfo.Id}");
        
        var downloader = new EasyDownload(buildInfo, true, _versionConfig.VersionsRootPath,
            _versionConfig.Info.VersionName, true);

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
            var itemsToDelete = new List<FileSystemInfo>();
            var rootDirInfo = new DirectoryInfo(runDirectory);

            CollectItemsToDelete(rootDirInfo, protectedPath, itemsToDelete);

            if (itemsToDelete.Count > 0)
            {
                int deletedCount = 0;
                int totalCount = itemsToDelete.Count;

                foreach (var item in itemsToDelete)
                {
                    try
                    {
                        if (item is DirectoryInfo subDir)
                        {
                            if (subDir.Exists) subDir.Delete(true);
                        }
                        else if (item is FileInfo file)
                        {
                            if (file.Exists) file.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"删除失败: {item.FullName}, 错误: {ex.Message}");
                    }
                    finally
                    {
                        deletedCount++;
                        int progressPercent = (int)((double)deletedCount / totalCount * 100);
                        Progress?.Report(new()
                        {
                            Message = $"正在删除文件 ({deletedCount}/{totalCount})",
                            Progress = progressPercent,
                            Step = InstanceUpdateStep.DeleteOld
                        });S
                    }
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

    private void CollectItemsToDelete(DirectoryInfo directory, string protectedPath, List<FileSystemInfo> itemsToDelete)
    {
        try
        {
            string currentDirPath = NormalizePath(directory.FullName);
            string formattedProtectedPath = NormalizePath(protectedPath);

            if (IsSameOrSubPath(currentDirPath, formattedProtectedPath))
            {
                return;
            }

            bool isParentOfProtected = IsAncestorPath(currentDirPath, formattedProtectedPath);

            foreach (var file in directory.GetFiles())
            {
                string filePath = NormalizePath(file.FullName);
                if (!IsSameOrSubPath(filePath, formattedProtectedPath))
                {
                    itemsToDelete.Add(file);
                }
            }

            foreach (var subDir in directory.GetDirectories())
            {
                string subDirPath = NormalizePath(subDir.FullName);

                if (IsSameOrSubPath(subDirPath, formattedProtectedPath))
                {
                    continue;
                }

                CollectItemsToDelete(subDir, protectedPath, itemsToDelete);

                if (!IsAncestorPath(subDirPath, formattedProtectedPath))
                {
                    itemsToDelete.Add(subDir);
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

    private bool IsAncestorPath(string parentPath, string childPath)
    {
        return childPath.StartsWith(parentPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}