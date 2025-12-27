using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Helper.PEFile;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Utils;

namespace BedrockBoot.Models.Pack.Game.Import;

public class PackInstaller
{
    public string PackFile { get; set; }
    public MinecraftBuildTypeVersion GameBuildType { get; private set; }
    public MinecraftGameTypeVersion GDKGameType { get; set; } = MinecraftGameTypeVersion.Release;
    public Action? ImportedAction { get; set; } = null;
    public IProgress<PackImportProgress> ImportProgress { get; set; } = new Progress<PackImportProgress>();
    public bool IsGDKUnknownBuildType { get; set; } = false;

    public PackInstaller(string filePath)
    {
        PackFile = filePath;
    }

    public async System.Threading.Tasks.Task Install(string dir, string gameName)
    {
        ImportProgress.Report(new PackImportProgress() { Progress = 10, StatusMessage = "判断文件类型..." });
        GameBuildType = PackAnalysis.GetPackBuildTypeWithFileHeader(PackFile);

        ImportProgress.Report(new PackImportProgress() { Progress = 100, StatusMessage = "判断文件类型完毕" });

        if (GameBuildType == MinecraftBuildTypeVersion.GDK)
            await InstallWithGDK(dir, gameName);
        if (GameBuildType == MinecraftBuildTypeVersion.UWP)
            await InstallWithUWP(dir, gameName);
    }

    #region UWP Installer

    private async System.Threading.Tasks.Task InstallWithUWP(string dir, string gameName)
    {
        var path = Path.Combine(dir, "bedrock_versions", gameName);
        var manifest = PackageIdentity.ParseFromXml(ExtractAppxManifestFromAppx(PackFile));
        var gameType = GetVersionTypeWithUWP(manifest.Name);

        // 确保目标目录的父目录存在
        var parentDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
        {
            GameName = gameName,
            Type = MinecraftBuildTypeVersion.UWP,
            InstallDstFolder = path,
            GameTypeVersion = gameType,
            FileFullPath = PackFile,
            ExtractionProgress = new Progress<DecompressProgress>((s) =>
            {
                ImportProgress.Report(new PackImportProgress()
                {
                    Progress = s.Percentage,
                    StatusMessage = $"解压文件中... ({s.Percentage:F2} %)"
                });
            }),
            DeployProgress = new Progress<DeploymentProgress>((s) =>
            {
                ImportProgress.Report(new PackImportProgress()
                {
                    Progress = s.percentage,
                    StatusMessage = $"部署游戏中 ({s.state.ToString()}) ({s.percentage:F2} %)"
                });
            })
        });

        GameInfoHelper.SaveVersionConfig(new VersionConfig()
        {
            Config = new VersionConfig.VersionConfigEntry(),
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = MinecraftBuildTypeVersion.UWP,
                Version = manifest.Version,
                VersionName = gameName,
                VersionType = gameType
            },
            VersionPath = path
        });

        if (ImportedAction != null)
            ImportedAction.Invoke();
    }

    private MinecraftGameTypeVersion GetVersionTypeWithUWP(string packName)
    {
        packName = packName.ToLower();
        if (packName.Contains("beta"))
            return MinecraftGameTypeVersion.Preview;
        return MinecraftGameTypeVersion.Release;
    }

    private string ExtractAppxManifestFromAppx(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        // 查找AppxManifest.xml文件
        var manifestEntry = archive.Entries
            .FirstOrDefault(e => e.FullName.EndsWith("AppxManifest.xml",
                StringComparison.OrdinalIgnoreCase));

        if (manifestEntry == null)
        {
            throw new FileNotFoundException("AppxManifest.xml not found in the archive");
        }

        // 读取到内存
        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #endregion

    #region GDK Installer

    private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private readonly HashSet<string> _processedFiles = new HashSet<string>();

    private async System.Threading.Tasks.Task<MinecraftGameTypeVersion> TryParseGDKGameType(
        MinecraftGameTypeVersion gameType = MinecraftGameTypeVersion.Beta)
    {
        // 从 Release 开始尝试
        var allTypes = new[] { MinecraftGameTypeVersion.Release, MinecraftGameTypeVersion.Preview };

        foreach (var typeToTry in allTypes)
        {
            string fileKey = $"{PackFile}_{typeToTry}";
            if (_processedFiles.Contains(fileKey))
            {
                // 如果已经处理过，跳过
                continue;
            }

            await _fileLock.WaitAsync();

            try
            {
                Console.WriteLine($"开始尝试检测 {typeToTry} 版本类型...");

                // 创建文件副本以避免占用问题
                string tempPackPath = await CreateTempFileCopy(PackFile);

                string tempGameName = $"_temp_{Guid.NewGuid():N}";
                string tempInstallPath = Path.Combine(PathsList.TempPath, tempGameName);

                // 确保临时目录存在
                if (!Directory.Exists(tempInstallPath))
                {
                    Directory.CreateDirectory(tempInstallPath);
                }

                bool shouldCleanup = true;

                try
                {
                    // 创建快速取消的令牌
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    bool detectedValidExe = false;

                    var progressHandler = new Progress<DecompressProgress>((progress) =>
                    {
                        // 当进度超过10%时，检查 Minecraft.Windows.exe 是否有效
                        if (progress.Percentage > 10 && !detectedValidExe)
                        {
                            string minecraftExePath = Path.Combine(tempInstallPath, "Minecraft.Windows.exe");
                            if (File.Exists(minecraftExePath))
                            {
                                if (PEFileValidator.IsValidEffectivePEFile(minecraftExePath))
                                {
                                    detectedValidExe = true;
                                    Console.WriteLine($"检测到有效的 Minecraft.Windows.exe 文件，{typeToTry} 版本类型有效");
                                    // 立即请求取消，因为已经找到有效文件
                                    if (!cts.IsCancellationRequested)
                                    {
                                        cts.Cancel();
                                    }
                                }
                            }
                        }
                    });

                    var installTask = GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
                    {
                        GameName = tempGameName,
                        Type = MinecraftBuildTypeVersion.GDK,
                        InstallDstFolder = tempInstallPath,
                        GameTypeVersion = typeToTry,
                        FileFullPath = tempPackPath, // 使用临时文件副本
                        ExtractionProgress = progressHandler,
                        CancellationToken = cts.Token
                    });

                    try
                    {
                        await installTask;
                        // 如果完整安装完成而没有取消，也检查exe文件
                        if (!detectedValidExe)
                        {
                            string minecraftExePath = Path.Combine(tempInstallPath, "Minecraft.Windows.exe");
                            if (File.Exists(minecraftExePath) &&
                                PEFileValidator.IsValidEffectivePEFile(minecraftExePath))
                            {
                                detectedValidExe = true;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        // 任务被取消，这是预期的，因为我们只想要解压10%
                    }

                    // 检查是否检测到有效exe
                    if (detectedValidExe)
                    {
                        Console.WriteLine($"{typeToTry} 版本类型检测成功");
                        _processedFiles.Add(fileKey);
                        return typeToTry; // 返回有效的版本类型
                    }
                    else
                    {
                        Console.WriteLine($"{typeToTry} 版本类型无效，继续尝试下一个...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{typeToTry} 版本检测过程中发生异常: {ex.Message}");

                    // 如果是文件访问错误，标记为已处理
                    if (ex is IOException && ex.Message.Contains("being used by another process"))
                    {
                        _processedFiles.Add(fileKey);
                        shouldCleanup = false;
                        // 等待后继续下一个类型
                        await System.Threading.Tasks.Task.Delay(1000);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    else
                    {
                        // 其他异常则抛出
                        throw;
                    }
                }
                finally
                {
                    if (shouldCleanup)
                    {
                        // 延迟清理临时目录
                        System.Threading.Tasks.Task.Delay(2000).ContinueWith(async _ =>
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                try
                                {
                                    if (Directory.Exists(tempInstallPath))
                                    {
                                        Directory.Delete(tempInstallPath, true);
                                        Console.WriteLine($"已清理临时目录: {tempInstallPath}");
                                    }
                                }
                                catch (Exception cleanupEx)
                                {
                                    Console.WriteLine($"清理临时目录失败: {cleanupEx.Message}");
                                }
                            }

                            // 清理临时包文件
                            await CleanupTempFile(tempPackPath);
                        });
                    }
                    else
                    {
                        // 即使shouldCleanup为false，也尝试清理临时包文件
                        await CleanupTempFile(tempPackPath);
                    }
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        // 如果所有类型都无效，返回Beta表示包无效
        Console.WriteLine("所有版本类型检测都失败，包无效");
        return MinecraftGameTypeVersion.Beta;
    }

    private async System.Threading.Tasks.Task<string> CreateTempFileCopy(string originalFilePath)
    {
        string tempPath = Path.Combine(PathsList.TempPath,
            $"temp_pack_{Guid.NewGuid()}{Path.GetExtension(originalFilePath)}");

        try
        {
            // 尝试复制文件，最多重试3次
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Copy(originalFilePath, tempPath, true);
                    Console.WriteLine($"创建临时文件副本: {tempPath}");
                    return tempPath;
                }
                catch (IOException) when (i < 2)
                {
                    Console.WriteLine($"复制文件失败，等待后重试 ({i + 1}/3)...");
                    await System.Threading.Tasks.Task.Delay(500);
                }
            }

            throw new IOException($"无法创建文件副本: {originalFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建临时文件副本失败: {ex.Message}");
            throw;
        }
    }

    private async System.Threading.Tasks.Task CleanupTempFile(string tempFilePath)
    {
        try
        {
            // 等待一段时间确保文件不再被使用
            await System.Threading.Tasks.Task.Delay(1000);

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                Console.WriteLine($"已清理临时文件: {tempFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理临时文件失败: {ex.Message}");
            // 不抛出异常，因为这只是一个清理操作
        }
    }

    private async System.Threading.Tasks.Task InstallWithGDK(string dir, string gameName)
    {
        // 创建文件副本以避免占用问题
        string tempFilePath = await CreateTempFileCopy(PackFile);

        try
        {
            var path = Path.Combine(dir, "bedrock_versions", gameName);

            // 确保目标目录的父目录存在
            var parentDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            // 检测版本类型
            var gameTypeVersion = GDKGameType;
            if (IsGDKUnknownBuildType)
            {
                Console.WriteLine("开始自动检测GDK版本类型...");

                gameTypeVersion = await TryParseGDKGameType();

                // 如果还是未知，尝试检测
                if (gameTypeVersion == MinecraftGameTypeVersion.Beta)
                {
                    throw new Exception("该包无效");
                }
            }

            Console.WriteLine($"最终确定的版本类型: {gameTypeVersion}");

            var jd = 0.00;
            // 使用临时文件进行安装
            await GlobalModel.BedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
            {
                GameName = gameName,
                Type = MinecraftBuildTypeVersion.GDK,
                InstallDstFolder = path,
                GameTypeVersion = gameTypeVersion,
                FileFullPath = tempFilePath,
                ExtractionProgress = new Progress<DecompressProgress>((progress) =>
                {
                    if (jd != progress.Percentage * 1.00)
                    {
                        jd = progress.Percentage * 1.00;
                        ImportProgress.Report(new PackImportProgress()
                        {
                            Progress = progress.Percentage,
                            StatusMessage = $"解压文件中... ({progress.Percentage:F2}%)"
                        });
                    }
                })
            });

            // 验证安装结果
            await VerifyInstallation(path, gameName, gameTypeVersion);

            Console.WriteLine($"GDK 版本安装成功: {gameName}");

            // 触发导入完成事件
            ImportedAction?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GDK 安装失败: {ex}");

            // 如果是文件占用错误，给出更明确的提示
            if (ex is IOException && ex.Message.Contains("being used by another process"))
            {
                throw new IOException($"文件被其他程序占用，请关闭可能使用该文件的程序后重试: {PackFile}", ex);
            }

            throw;
        }
        finally
        {
            // 清理临时文件
            await CleanupTempFile(tempFilePath);
        }
    }

    private async System.Threading.Tasks.Task VerifyInstallation(string installPath, string gameName,
        MinecraftGameTypeVersion gameType)
    {
        var manifestPath = Path.Combine(installPath, "appxmanifest.xml");

        // 等待文件出现（最多5秒）
        for (int i = 0; i < 10 && !File.Exists(manifestPath); i++)
        {
            await System.Threading.Tasks.Task.Delay(500);
        }

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("安装完成但未找到 manifest 文件", manifestPath);
        }

        var manifestContent = await File.ReadAllTextAsync(manifestPath);
        var manifest = PackageIdentity.ParseFromXml(manifestContent);

        // 保存版本配置
        GameInfoHelper.SaveVersionConfig(new VersionConfig()
        {
            Config = new VersionConfig.VersionConfigEntry(),
            Info = new VersionConfig.VersionInfo()
            {
                BuildType = MinecraftBuildTypeVersion.GDK,
                Version = manifest.Version,
                VersionName = gameName,
                VersionType = GDKGameType
            },
            VersionPath = installPath
        });

        Console.WriteLine($"安装验证完成: {gameName} (版本: {manifest.Version}, 类型: {gameType})");
    }

    private MinecraftGameTypeVersion GetVersionTypeWithGDK(string packName)
    {
        if (string.IsNullOrEmpty(packName))
            return MinecraftGameTypeVersion.Release;

        packName = packName.ToLowerInvariant();

        if (packName.Contains("preview") || packName.Contains("beta"))
            return MinecraftGameTypeVersion.Preview;

        return MinecraftGameTypeVersion.Release;
    }

    #endregion
}