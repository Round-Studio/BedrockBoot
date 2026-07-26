using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.ResourcePack;
using BedrockBoot.Services;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using Round.SDK.Entity;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Game.Integration;

public class IntegrationInstaller
{
    public IntegrationInstaller(string pack)
    {
        PackageFile = pack;
    }

    public string PackageFile { get; set; }
    public IProgress<InstallIntegrationProgress>? IntegrationProgress { get; set; }

    public PackInfo? GetPackInfo()
    {
        var packJson = ZipHelper.GetTextFileContent(PackageFile, "pack.json");
        if (string.IsNullOrEmpty(packJson)) throw new Exception("文件非可用的整合包文件");

        return JsonSerializer.Deserialize<PackInfo>(packJson);
    }

    public async Task BeginInstaller(string installFolder, string installName, CancellationToken token = default)
    {
        var path = Path.Combine(PathsList.TempPath, $"integration_{Guid.NewGuid().ToString().Replace("-", "")}");

        var info = GetPackInfo();

        if (info == null) throw new Exception("无法解析整合包信息");

        var gameVersions = VersionHelper.GetVersions()
            .Find(x => x.ID.Replace(".", "") ==
                       info.VersionInfo.Version.Replace(".", ""));

        if (gameVersions == null) throw new Exception("无法获取整合包目标游戏版本");

        var unZip = 0.00;
        var isComp = false;
        var downloader = new EasyDownload(gameVersions, true, installFolder, installName)
        {
            DownloadProgress = (s, p) =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = p.Percentage,
                    Message = $"{s} ({p.Speed} / s)",
                    Status = InstallIntegrationProgressType.DownloadingFile
                });
            },
            MergeProgress = (s, p) =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = p,
                    Message = "合并文件",
                    Status = InstallIntegrationProgressType.DownloadedFile
                });
            },
            ExtractionProgress = (s, p) =>
            {
                if (Math.Abs(p - unZip) > 0.01)
                {
                    unZip = p;
                    IntegrationProgress?.Report(new InstallIntegrationProgress
                    {
                        Progress = p,
                        Message = "解压文件",
                        Status = InstallIntegrationProgressType.Installing
                    });
                }
            },
#if WINDOWS
            DeploymentProgress = (s, p) =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = p.percentage,
                    Message = $"安装游戏 {p.state}",
                    Status = InstallIntegrationProgressType.Installing
                });
            },
#endif
            StatusText = text =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = -1,
                    Message = text,
                    Status = InstallIntegrationProgressType.Installing
                });
            },
            ErrorOccurred = (title, message, ex) =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = 0,
                    Message = $"{title}: {message} {ex}",
                    Status = InstallIntegrationProgressType.Failed
                });
            },
            Completed = gameConfig =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = 100,
                    Message = "实例安装完成",
                    Status = InstallIntegrationProgressType.Installed
                });

                if (!isComp)
                {
                    isComp = true;
                    InstallPack(path, gameConfig);
                }
            }
        };

        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 0,
            Message = "获取 URL",
            Status = InstallIntegrationProgressType.GetUrl
        });

        if (CoreGlobal.BedrockCore == null)
            CoreGlobal.BedrockCore = new BedrockCore
            {
#if WINDOWS
                Options = new CoreOptions
                {
                    IsAutoCompleteVC = true,
                    IsAutoOpenDevelopment = true,
                    IsCheckMD5 = true
                }
#endif
            };

        var url = await CoreGlobal.BedrockCore.GetPackageUri(gameVersions, Architecture.X64);

        // 添加错误处理
        // 整合包安装固定启用缓存（isUsePack: true）：
        // 即使无法获取下载地址，只要本地或全局索引中存在该版本的缓存包，也允许继续安装
        if (string.IsNullOrEmpty(url))
        {
            var localPack = Path.Combine(installFolder, "version_save", $"{gameVersions.ID}.insPack");
            var hasCache = File.Exists(localPack) ||
                           BedrockBoot.Core.Models.Helper.GamePackageCacheIndex.Find(
                               gameVersions.ID, gameVersions.BuildType.ToString()) != null;

            if (!hasCache)
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = 0,
                    Message = "无法获取下载地址",
                    Status = InstallIntegrationProgressType.Failed
                });
                return;
            }
        }

        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 5,
            Message = "开始下载游戏",
            Status = InstallIntegrationProgressType.DownloadingFile
        });

        try
        {
            await downloader.InstallAsync(url, token);
        }
        catch (OperationCanceledException)
        {
            // 用户主动取消，不作为下载失败上报
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = "安装已取消",
                Status = InstallIntegrationProgressType.Failed
            });
        }
        catch (Exception ex)
        {
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = $"下载失败: {ex.Message}",
                Status = InstallIntegrationProgressType.Failed
            });
        }
    }

    private void InstallPack(string path, VersionConfig gameConfig)
    {
        // 此前该方法为 async Task 且调用处丢弃返回值：
        // 解压/导入过程中任何异常都会被无声吞掉，任务永远停在安装中。
        try
        {
            InstallPackCore(path, gameConfig);

            // 无论整合包内包含哪些内容（即使为空包），都必须上报 Success，
            // 否则任务项永远不会被移除
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 100,
                Message = "安装完成",
                Status = InstallIntegrationProgressType.Success
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"安装整合包内容失败: {ex}");
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = $"安装整合包内容失败: {ex.Message}",
                Status = InstallIntegrationProgressType.Failed
            });
        }
        finally
        {
            // 清理解压用的临时目录
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"清理整合包临时目录失败: {ex.Message}");
            }
        }
    }

    private void InstallPackCore(string path, VersionConfig gameConfig)
    {
        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 80,
            Message = "解压整合包文件",
            Status = InstallIntegrationProgressType.Uninstalling
        });

        ZipHelper.ExtractZipFile(PackageFile, path);

        // 安装资源包
        if (Directory.Exists(Path.Combine(path, "packs", "resource_packs")))
        {
            var packManager = new ResourcePackManager(gameConfig);
            packManager.GetAllPack();
            var files = Directory.GetFiles(Path.Combine(path, "packs", "resource_packs")).ToList();
            var count = 0;
            files.ForEach(file =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = (double)count / files.Count * 100.00,
                    Message = "解压整合包资源包文件",
                    Status = InstallIntegrationProgressType.Uninstalling
                });
                packManager.AddRangePacks(new List<string> { file });
                count++;
            });
        }

        // 安装行为包
        if (Directory.Exists(Path.Combine(path, "packs", "behavior_packs")))
        {
            var packManager = new ResourcePackManager(gameConfig);
            packManager.GetAllPack();
            var files = Directory.GetFiles(Path.Combine(path, "packs", "behavior_packs")).ToList();
            var count = 0;
            files.ForEach(file =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = (double)count / files.Count * 100.00,
                    Message = "解压整合包行为包文件",
                    Status = InstallIntegrationProgressType.Uninstalling
                });
                packManager.AddRangePacks(new List<string> { file });
                count++;
            });
        }

        // 安装皮肤包
        // 注意：打包端会把皮肤包放进 packs/skin_packs，
        // 此前安装端没有对应分支，皮肤包被静默丢弃
        if (Directory.Exists(Path.Combine(path, "packs", "skin_packs")))
        {
            var packManager = new ResourcePackManager(gameConfig);
            packManager.GetAllPack();
            var files = Directory.GetFiles(Path.Combine(path, "packs", "skin_packs")).ToList();
            var count = 0;
            files.ForEach(file =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = (double)count / files.Count * 100.00,
                    Message = "解压整合包皮肤包文件",
                    Status = InstallIntegrationProgressType.Uninstalling
                });
                packManager.AddRangePacks(new List<string> { file });
                count++;
            });
        }

        // 导入世界
        if (Directory.Exists(Path.Combine(path, "worlds")))
        {
            var packManager = new ArchiveCheck(gameConfig);
            var files = Directory.GetFiles(Path.Combine(path, "worlds")).ToList();
            var count = 0;
            files.ForEach(file =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = (double)count / files.Count * 100.00,
                    Message = "解压整合包存档文件",
                    Status = InstallIntegrationProgressType.Uninstalling
                });
                packManager.ImportWorldPack(file);
                count++;
            });
        }

        // 安装Mods
        if (File.Exists(Path.Combine(path, "mods", "mods.json")))
        {
            var modConf =
                new ConfigEntity<Dictionary<string, PackModInfo>>(Path.Combine(path, "mods", "mods.json"));

            var modManager = new ModsManager(gameConfig);
            modConf.Data?.ToList().ForEach(mod =>
            {
                var modFilePath = Path.Combine(path, "mods", Path.GetFileName(mod.Key));
                if (File.Exists(modFilePath))
                {
                    var newFile = Path.Combine(gameConfig.VersionPath, "config", "BedrockBoot2", "mods",
                        Path.GetFileName(mod.Key));

                    // 确保目录存在
                    Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                    File.Copy(modFilePath, newFile, true);

                    modManager.AddMod(new ModInfo
                    {
                        File = newFile,
                        IsPreLoad = mod.Value.IsPreLoad,
                        InjectDelay = mod.Value.Delay
                    });
                }
            });
        }
    }
}