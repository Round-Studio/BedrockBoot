using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Integration;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Models.Pack.Game.Mods;
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

    public async Task BeginInstaller(string installFolder, string installName)
    {
        var path = Path.Combine(PathsList.TempPath, $"integration_{Guid.NewGuid().ToString().Replace("-", "")}");
        Directory.CreateDirectory(path);

        var info = GetPackInfo();

        if (info == null) throw new Exception("无法解析整合包信息");

        var gameVersions = VersionHelper.GetVersions()
            .Find(x => x.ID.Replace(".", "") ==
                       info.VersionInfo.Version.Replace(".", ""));

        if (gameVersions == null) throw new Exception("无法获取整合包目标游戏版本");

        var unZip = 0.00;
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
            DeploymentProgress = (s, p) =>
            {
                IntegrationProgress?.Report(new InstallIntegrationProgress
                {
                    Progress = p.percentage,
                    Message = $"安装游戏 {p.state}",
                    Status = InstallIntegrationProgressType.Installing
                });
            },
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
                    Status = InstallIntegrationProgressType.Success
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

                InstallPack(path, gameConfig);
            }
        };

        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 0,
            Message = "获取 URL",
            Status = InstallIntegrationProgressType.GetUrl
        });

        if (GlobalModel.BedrockCore == null)
            GlobalModel.BedrockCore = new BedrockCore
            {
                Options = new CoreOptions
                {
                    IsAutoCompleteVC = true,
                    IsAutoOpenDevelopment = true,
                    IsAutoCompleteGameInput = true,
                    IsCheckMD5 = true
                }
            };

        var url = await GlobalModel.BedrockCore.GetPackageUri(gameVersions, Architecture.X64);

        // 添加错误处理
        if (string.IsNullOrEmpty(url))
        {
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = "无法获取下载地址",
                Status = InstallIntegrationProgressType.Success
            });
            return;
        }

        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 5,
            Message = "开始下载游戏",
            Status = InstallIntegrationProgressType.DownloadingFile
        });

        try
        {
            await downloader.InstallAsync(url);
        }
        catch (Exception ex)
        {
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = $"下载失败: {ex.Message}",
                Status = InstallIntegrationProgressType.Success
            });
        }
    }

    private void InstallPack(string path, VersionConfig gameConfig)
    {
        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 80,
            Message = "解压整合包文件",
            Status = InstallIntegrationProgressType.Uninstalling
        });

        try
        {
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
                        Progress = ((double)count / files.Count) * 100.00,
                        Message = "解压整合包资源包文件",
                        Status = InstallIntegrationProgressType.Uninstalling
                    });
                    packManager.AddRangePacks(new() { file });
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
                        Progress = ((double)count / files.Count) * 100.00,
                        Message = "解压整合包行为包文件",
                        Status = InstallIntegrationProgressType.Uninstalling
                    });
                    packManager.AddRangePacks(new() { file });
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
                        Progress = ((double)count / files.Count) * 100.00,
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
                modConf.Data.ToList().ForEach(mod =>
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
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            IntegrationProgress?.Report(new InstallIntegrationProgress
            {
                Progress = 0,
                Message = $"整合包安装失败: {ex}",
                Status = InstallIntegrationProgressType.Success
            });
            return;
        }
        finally
        {
            // 清理临时文件
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
                /* 忽略清理错误 */
            }
        }

        IntegrationProgress?.Report(new InstallIntegrationProgress
        {
            Progress = 100,
            Message = "安装完成",
            Status = InstallIntegrationProgressType.Success
        });
    }
}