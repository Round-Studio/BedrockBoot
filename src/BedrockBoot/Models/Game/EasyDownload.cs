using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;
using Round.SDK.Helper;
using DownloadProgress = BedrockBoot.Base.Entry.DownloadProgress;

namespace BedrockBoot.Services
{
    public class EasyDownload
    {
        public BuildInfo BuildInfo { get; set; }
        public string InstallFolder { get; set; }
        public string GameName { get; set; }
        
        // 进度报告回调
        public Action<string, double> DownloadProgress { get; set; }
        public Action<string> DownloadSpeed { get; set; }
        public Action<string, double> MergeProgress { get; set; } // 新增：合并进度回调
        public Action<string, double> ExtractionProgress { get; set; }
        public Action<string, DeploymentProgress> DeploymentProgress { get; set; }
        public Action<string> StatusText { get; set; }
        public Action<InstallStates> InstallStateChanged { get; set; }
        public Action<string, string, Exception> ErrorOccurred { get; set; }
        public Action Completed { get; set; }

        public EasyDownload(BuildInfo info, string dir, string gameName)
        {
            BuildInfo = info;
            InstallFolder = dir;
            GameName = gameName;
        }

        public async Task InstallAsync(string url)
        {
            Console.WriteLine($@"下载游戏，地址：{url}");
            try
            {
                // 1. 准备下载目录
                PrepareDownloadDirectory();
                
                // 2. 下载游戏包
                var packagePath = await DownloadPackageAsync(url);
                
                // 3. 验证包完整性
                if (!await ValidatePackageAsync(packagePath))
                {
                    return;
                }
                
                // 4. 标记合并完成
                OnMergeComplete();
                
                // 5. 安装包
                await InstallPackageAsync(packagePath);
                
                Completed?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("安装失败", $"游戏 {GameName} 安装失败", ex);
            }
        }

        private void PrepareDownloadDirectory()
        {
            var versionSavePath = Path.Combine(InstallFolder, "version_save");
            if (!Directory.Exists(versionSavePath))
            {
                Directory.CreateDirectory(versionSavePath);
            }
        }

        private async Task<string> DownloadPackageAsync(string url)
        {
            var packagePath = Path.Combine(InstallFolder, "version_save", $"{BuildInfo.ID}.insPack");
            
            // 如果文件已存在且MD5校验通过，则跳过下载
            if (File.Exists(packagePath) && await CheckMD5(packagePath, false))
            {
                StatusText?.Invoke("使用缓存包");
                DownloadProgress.Invoke("", 100);
                OnMergeComplete(); // 使用缓存时也触发合并完成
                return packagePath;
            }
            
            StatusText?.Invoke("正在下载游戏包...");
            var downloader = new MultiThreadDownloader(GlobalModel.Config.Data.DownloadChunkCount, 1024);
            var speedCalculator = new DownloadSpeedCalculator();

            await downloader.DownloadAsync(url, packagePath, new Progress<DownloadProgress>(progress =>
            {
                DownloadProgress?.Invoke($"下载游戏 ({progress.ProgressPercentage:F2}%)", 
                    progress.ProgressPercentage);
                
                var speed = SizeHelper.FormatBytes(
                    speedCalculator.UpdateSpeed(progress.DownloadedBytes, progress.TotalBytes));
                DownloadSpeed?.Invoke($"{speed}/s");
            }));

            return packagePath;
        }

        private async Task<bool> ValidatePackageAsync(string packagePath)
        {
            StatusText?.Invoke("正在验证包完整性...");
            
            if (!await CheckMD5(packagePath))
            {
                ErrorOccurred?.Invoke("无效包", "当前下载的包无效，请重新下载", null);
                return false;
            }
            
            return true;
        }

        private void OnMergeComplete()
        {
            // 合并完成回调
            MergeProgress?.Invoke("合并完成", 100);
            StatusText?.Invoke("本地安装中...");
        }

        private async Task InstallPackageAsync(string packagePath)
        {
            string installDir = Path.Combine(InstallFolder, "bedrock_versions", GameName);

            await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
            {
                FileFullPath = packagePath,
                GameName = GameName,
                InstallDstFolder = installDir,
                GameTypeVersion = BuildInfo.Type,
                Type = BuildInfo.BuildType,
                ExtractionProgress = new Progress<DecompressProgress>(progress =>
                {
                    ExtractionProgress?.Invoke($"解压文件 ({progress.Percentage:F2}%)", 
                        progress.Percentage);
                }),
                DeployProgress = new Progress<DeploymentProgress>(progress =>
                {
                    DeploymentProgress?.Invoke($"部署游戏 ({progress.state} [{progress.percentage}%])", 
                        progress);
                }),
                InstallStates = new Progress<InstallStates>(states =>
                {
                    InstallStateChanged?.Invoke(states);
                    HandleInstallState(states, installDir);
                })
            });
        }

        private void HandleInstallState(InstallStates state, string installDir)
        {
            switch (state)
            {
                case InstallStates.Extracted:
                    if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
                    {
                        SaveVersionConfig(installDir);
                        Completed?.Invoke();
                    }
                    break;
                    
                case InstallStates.Registering:
                    // 对于非GDK版本，在注册时保存配置
                    if (BuildInfo.BuildType != MinecraftBuildTypeVersion.GDK)
                    {
                        SaveVersionConfig(installDir);
                    }
                    break;
                    
                case InstallStates.Registered:
                    if (BuildInfo.BuildType != MinecraftBuildTypeVersion.GDK)
                    {
                        Completed?.Invoke();
                    }
                    break;
            }
        }

        private void SaveVersionConfig(string installDir)
        {
            GameInfoHelper.SaveVersionConfig(new VersionConfig()
            {
                VersionPath = installDir,
                Info = new VersionConfig.VersionInfo()
                {
                    BuildType = BuildInfo.BuildType,
                    Version = BuildInfo.ID,
                    VersionName = GameName,
                    VersionType = BuildInfo.Type
                }
            });
        }

        public async Task<bool> CheckMD5(string file, bool showError = true)
        {
            try
            {
                var fileMD5 = await ComputeFileMD5.ComputeFileMD5Async(file);
                
                foreach (var variation in BuildInfo.Variations)
                {
                    if (variation.MD5 == fileMD5)
                    {
                        return true;
                    }
                }
                
                if (showError)
                {
                    ErrorOccurred?.Invoke("无效包", "当前下载的包无效，请重新下载", null);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("MD5校验失败", "验证文件完整性时出错", ex);
                return false;
            }
        }

        public static async Task<List<GameDownloadUrlInfo>> GetPackageUrls(BuildInfo buildInfo)
        {
            try
            {
                var url = await GlobalModel.BedrockCore.GetPackageUri(buildInfo, Architecture.X64);
                Console.WriteLine($@"原始地址：{url}");
                
                var res = new List<GameDownloadUrlInfo>();
                var uri = new Uri(url);

                if (buildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
                {
                    var router = uri.AbsolutePath;

                    SourceList.GameFileDownloadSource.ForEach(s =>
                    {
                        res.Add(new GameDownloadUrlInfo()
                        {
                            Host = s.Host,
                            Url = s.Url.Replace("{router}", router)
                        });
                    });
                }
                else
                {
                    res.Add(new GameDownloadUrlInfo()
                    {
                        Host = uri.Host,
                        Url = url
                    });
                }
                
                return res;
            }
            catch
            {
                return null;
            }
        }
    }
}