using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.LeviLamina.Base.Entry.Manifest;
using BedrockBoot.LeviLamina.Base.Entry.Porgress;
using BedrockBoot.LeviLamina.Base.Enum;
using BedrockBoot.LeviLamina.Global;
using BedrockBoot.LeviLamina.Models.ApiClient;
using Octokit;
using Round.SDK.Entity;
using Round.SDK.Helper;
using System.Collections.Concurrent;
using Round.SDK.Enum;
using Round.SDK.Helper.IO;

namespace BedrockBoot.LeviLamina.Models.Installer;

public class LeviLaminaInstaller
{
    public VersionConfig VersionInfo { get; private set; }
    public IProgress<InstallerProgress> Progress { get; set; }

    // 添加缓存相关的字段
    private readonly ConcurrentDictionary<string, bool> _downloadedFiles = new();
    private readonly string _cacheIndexFile;
    private readonly bool _useCache;

    public LeviLaminaInstaller(VersionConfig versionConfig, bool useCache = true)
    {
        VersionInfo = versionConfig;
        _useCache = useCache;
        _cacheIndexFile = Path.Combine(PathList.LeviLaminaCacheFolder, "cache_index.txt");
        
        // 初始化缓存目录
        InitializeCache();
        var modsPath = Path.Combine(VersionInfo.VersionPath, "mods");
        var targetModsPath = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods");
        
        // 检查并创建目标目录
        if (!Directory.Exists(targetModsPath))
        {
            Directory.CreateDirectory(targetModsPath);
            Console.WriteLine($@"创建目标mods目录: {targetModsPath}");
        }
        
        // 检查当前mods目录的状态
        if (Directory.Exists(modsPath))
        {
            try
            {
                // 尝试获取链接信息，判断是否为符号链接
                var linkInfo = DirectoryLinkChecker.CheckFolderType(modsPath);
                
                if (linkInfo == DirectoryType.Folder)
                {
                    // 如果是普通文件夹，删除它
                    Console.WriteLine($@"删除普通mods文件夹: {modsPath}");
                    Directory.Delete(modsPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"检查mods目录失败，尝试删除: {ex.Message}");
                try { Directory.Delete(modsPath, true); } catch { }
            }
        }
        
        // 创建符号链接
        Console.WriteLine($@"创建符号链接: {modsPath} -> {targetModsPath}");
        try
        {
            Directory.CreateSymbolicLink(modsPath, targetModsPath);
        }catch{}
    }

    private void InitializeCache()
    {
        try
        {
            if (!Directory.Exists(PathList.LeviLaminaCacheFolder))
            {
                Directory.CreateDirectory(PathList.LeviLaminaCacheFolder);
            }
            
            if (!Directory.Exists(PathList.LeviLaminaSourceFolder))
            {
                Directory.CreateDirectory(PathList.LeviLaminaSourceFolder);
            }
            
            if (!Directory.Exists(PathList.LeviLaminaTempFolder))
            {
                Directory.CreateDirectory(PathList.LeviLaminaTempFolder);
            }

            // 加载已下载文件索引
            if (_useCache && File.Exists(_cacheIndexFile))
            {
                var cachedFiles = File.ReadAllLines(_cacheIndexFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();
                
                foreach (var file in cachedFiles)
                {
                    _downloadedFiles[file] = true;
                }
            }
        }
        catch (Exception ex)
        {
            ReportError($"初始化缓存失败: {ex.Message}");
            throw;
        }
    }

    private void SaveToCacheIndex(string fileName)
    {
        try
        {
            if (!_useCache) return;
            
            _downloadedFiles[fileName] = true;
            File.AppendAllText(_cacheIndexFile, fileName + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"保存缓存索引失败: {ex.Message}");
        }
    }

    public async Task<List<string>> GetVersions()
    {
        try
        {
            var lmaDb = await new LeviLaminaManifestApi().GetVersions();
            var result = new List<string>();
            
            var targetVersion = VersionInfo.Info.Version.Replace(".", "");
            lmaDb.Versions.Keys.ToList().ForEach(x =>
            {
                if (targetVersion.StartsWith(x.Replace(".", "")))
                {
                    result = lmaDb.Versions[x];
                }
            });

            if (result.Count <= 0)
            {
                var errorMsg = $"版本 {VersionInfo.Info.Version} 不适用于 LeviLamina";
                ReportError(errorMsg);
                throw new NullReferenceException(errorMsg);
            }

            return result;
        }
        catch (Exception ex)
        {
            ReportError($"获取版本列表失败: {ex.Message}");
            throw;
        }
    }

    public async Task InstallLeviLamina(string lmaVersion)
    {
        try
        {
            Progress?.Report(new()
            {
                Message = "开始安装 LeviLamina...",
                Progress = 0,
                Status = InstallerStatus.DownloadSource
            });

            // 检查缓存中是否已有该版本
            if (_useCache && await CheckCachedVersion(lmaVersion))
            {
                Progress?.Report(new()
                {
                    Message = "使用缓存安装...",
                    Progress = 10,
                    Status = InstallerStatus.Processed
                });
                
                if (await InstallFromCache(lmaVersion))
                {
                    Progress?.Report(new()
                    {
                        Message = "缓存安装完成",
                        Status = InstallerStatus.Complete,
                        Progress = 100
                    });
                    return;
                }
                
                Progress?.Report(new()
                {
                    Message = "缓存安装失败，开始重新下载...",
                    Progress = 20,
                    Status = InstallerStatus.DownloadSource
                });
            }

            await DownloadAndInstallFresh(lmaVersion);

            Progress?.Report(new()
            {
                Message = "安装完成",
                Status = InstallerStatus.Complete,
                Progress = 100
            });
        }
        catch (Exception ex)
        {
            ReportError($"安装过程中出现错误: {ex.Message}");
            throw;
        }
    }

    private async Task<bool> CheckCachedVersion(string lmaVersion)
    {
        try
        {
            var sourcePath = Path.Combine(PathList.LeviLaminaSourceFolder, $"{lmaVersion}.zip");
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            // 检查提取的临时文件夹是否存在
            var tmpFolder = Path.Combine(PathList.LeviLaminaTempFolder, $"ll_{lmaVersion}");
            if (!Directory.Exists(tmpFolder))
            {
                return false;
            }

            var llJson = Path.Combine(tmpFolder, $"LeviLamina-{lmaVersion}", "tooth.json");
            if (!File.Exists(llJson))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> InstallFromCache(string lmaVersion)
    {
        try
        {
            var tmpFolder = Path.Combine(PathList.LeviLaminaTempFolder, $"ll_{lmaVersion}");
            var llJson = Path.Combine(tmpFolder, $"LeviLamina-{lmaVersion}", "tooth.json");
        
            if (!File.Exists(llJson))
            {
                ReportError("找不到缓存中的 tooth.json 文件");
                return false;
            }
        
            var llManifest = new ConfigEntity<ToothManifest>(llJson, false);
            var depInfo = llManifest.Data.Variants[1];
        
            if (string.IsNullOrEmpty(depInfo.Label) || depInfo.Label != "client")
            {
                ReportError("缓存文件无效：非客户端文件");
                return false;
            }

            var deps = await GetDependenceDownloadUrlsAsync(depInfo);
        
            // 添加LeviLamina本身的URL - 这是关键修复
            var allUrls = depInfo.Assets
                .SelectMany(asset => asset.Urls
                    .Select(url => url
                        .Replace("{{tooth}}", llManifest.Data.Tooth)
                        .Replace("{{version}}", llManifest.Data.Version)))
                .ToList();

            allUrls.ForEach(url => deps.Add(DependenciesType.LeviLamina, url));

            // 检查所有依赖文件（包括LeviLamina本身）是否都存在
            foreach (var dep in deps)
            {
                var fileName = GetCleanFileNameFromUri(dep.Value);
                var cachePath = Path.Combine(PathList.LeviLaminaCacheFolder, fileName);
            
                if (!File.Exists(cachePath))
                {
                    Console.WriteLine($@"缓存文件缺失: {fileName} - {dep.Key}");
                    return false;
                }
            }

            // 如果所有文件都存在，从缓存安装
            await ProcessDependenciesFromCache(deps, lmaVersion);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"缓存安装失败: {ex.Message}");
            return false;
        }
    }

    private async Task ProcessDependenciesFromCache(Dictionary<DependenciesType, string> deps, string lmaVersion)
    {
        var tasks = new List<Task>();
        
        foreach (var dep in deps)
        {
            var fileName = GetCleanFileNameFromUri(dep.Value);
            var cachePath = Path.Combine(PathList.LeviLaminaCacheFolder, fileName);
            
            var task = Task.Run(() =>
            {
                try
                {
                    ProcessDependencyFile(dep.Key, cachePath);
                    Console.WriteLine($@"{dep.Key} 从缓存安装完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"{dep.Key} 缓存处理失败: {ex.Message}");
                    throw;
                }
            });
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks);
    }

    private async Task DownloadAndInstallFresh(string lmaVersion)
    {
        Progress?.Report(new()
        {
            Message = "下载 LeviLamina 源码...",
            Progress = 0,
            Status = InstallerStatus.DownloadSource
        });

        var sourceUrl = SourceList.LeviLaminaSource.Replace("{version}", lmaVersion);
        var sourcePath = Path.Combine(PathList.LeviLaminaSourceFolder, $"{lmaVersion}.zip");

        Console.WriteLine($@"下载源: {sourceUrl}");    
        
        // 下载源码
        await DownloadWithRetry(sourceUrl, sourcePath, InstallerStatus.DownloadSource, "LeviLamina 清单", 3);    
        
        Progress?.Report(new()
        {
            Message = "下载 LeviLamina 源码...",
            Progress = 80,
            Status = InstallerStatus.DownloadSource
        });

        // 提取源码
        var tmpFolder = Path.Combine(PathList.LeviLaminaTempFolder, $"ll_{lmaVersion}");
        if (Directory.Exists(tmpFolder))
        {
            Directory.Delete(tmpFolder, true);
        }
        
        try
        {
            ZipHelper.ExtractZipFile(sourcePath, tmpFolder, true);
        }
        catch (Exception ex)
        {
            ReportError($"解压源码失败: {ex.Message}");
            throw;
        }

        var llJson = Path.Combine(tmpFolder, $"LeviLamina-{lmaVersion}", "tooth.json");
        if (!File.Exists(llJson))
        {
            ReportError($"找不到 tooth.json 文件");
            throw new FileNotFoundException("找不到 tooth.json 文件", llJson);
        }
        
        Progress?.Report(new()
        {
            Message = "下载 LeviLamina 源码...",
            Progress = 100,
            Status = InstallerStatus.DownloadSource
        });

        var llManifest = new ConfigEntity<ToothManifest>(llJson, false);
        var depInfo = llManifest.Data.Variants[1];
        
        if (string.IsNullOrEmpty(depInfo.Label) || depInfo.Label != "client")
        {
            ReportError("非客户端文件");
            throw new InvalidOperationException("非客户端文件");
        }

        var deps = await GetDependenceDownloadUrlsAsync(depInfo);
        
        // 添加LeviLamina本身的URL
        var allUrls = depInfo.Assets
            .SelectMany(asset => asset.Urls
                .Select(url => url
                    .Replace("{{tooth}}", llManifest.Data.Tooth)
                    .Replace("{{version}}", llManifest.Data.Version)))
            .ToList();

        allUrls.ForEach(url => deps.Add(DependenciesType.LeviLamina, url));

        // 下载并处理所有依赖
        await DownloadAndProcessDependencies(deps);
    }

    private async Task DownloadAndProcessDependencies(Dictionary<DependenciesType, string> deps)
    {
        var tasks = new List<Task>();
        var errors = new ConcurrentBag<Exception>();
        
        foreach (var dep in deps)
        {
            var fileName = GetCleanFileNameFromUri(dep.Value);
            var cachePath = Path.Combine(PathList.LeviLaminaCacheFolder, fileName);
            
            var task = Task.Run(async () =>
            {
                try
                {
                    // 检查缓存
                    if (_useCache && File.Exists(cachePath))
                    {
                        Console.WriteLine($@"{dep.Key} 使用缓存文件: {fileName}");
                        Progress?.Report(new()
                        {
                            Message = $"{dep.Key} 使用缓存",
                            Status = GetStatusFromDepType(dep.Key),
                            Progress = 100
                        });
                    }
                    else
                    {
                        // 下载文件
                        await DownloadWithRetry(
                            dep.Value, 
                            cachePath, 
                            GetStatusFromDepType(dep.Key),
                            $"{dep.Key}",
                            3
                        );
                        
                        // 保存到缓存索引
                        SaveToCacheIndex(fileName);
                    }

                    // 处理文件
                    await ProcessDependencyFile(dep.Key, cachePath);
                    
                    Progress?.Report(new()
                    {
                        Message = $"{dep.Key} 处理完成",
                        Status = InstallerStatus.Processed,
                        Progress = 100
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"{dep.Key} 处理失败: {ex.Message}");
                    errors.Add(ex);
                    
                    // 删除可能损坏的缓存文件
                    try
                    {
                        if (File.Exists(cachePath))
                            File.Delete(cachePath);
                    }
                    catch { }
                    
                    ReportError($"{dep.Key} 处理失败: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        
        if (errors.Count > 0)
        {
            throw new AggregateException("一个或多个组件处理失败", errors.ToList());
        }
    }

    private async Task ProcessDependencyFile(DependenciesType depType, string filePath)
    {
        switch (depType)
        {
            case DependenciesType.LeviLamina:
                ZipHelper.ExtractZipFile(filePath, Path.Combine(VersionInfo.VersionPath, "mods"), true);
                break;

            case DependenciesType.CrashLogger:
                var targetDir = Path.Combine(VersionInfo.VersionPath, "mods", "LeviLamina");
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);
                ZipHelper.ExtractZipFile(filePath, targetDir, true);
                break;

            case DependenciesType.BedrockRtd:
                var rtdFile = Path.Combine(VersionInfo.VersionPath, "bedrock_runtime_data");
                if (File.Exists(rtdFile))
                    File.Delete(rtdFile);
                ZipHelper.ExtractZipFile(filePath, VersionInfo.VersionPath, true);
                break;

            case DependenciesType.PreLoader:
                await InstallPreLoader(filePath);
                break;
        }
    }

    private async Task InstallPreLoader(string preloaderPath)
    {
        try
        {
            var preloaderFile = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods", "PreLoader.dll");
            var modsConfigPath = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods.json");
            
            // 确保目录存在
            var modsDir = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods");
            if (!Directory.Exists(modsDir))
                Directory.CreateDirectory(modsDir);
            
            // 更新mods.json
            if (File.Exists(modsConfigPath))
            {
                var conf = new ConfigEntity<List<ModInfo>>(modsConfigPath);
                if (!conf.Data.Any(m => m.File.EndsWith("PreLoader.dll")))
                {
                    conf.Data.Add(new ModInfo()
                    {
                        File = preloaderFile,
                        IsPreLoad = true,
                        InjectDelay = 0
                    });
                    conf.Save();
                }
            }
            
            // 删除旧文件
            if (File.Exists(preloaderFile))
                File.Delete(preloaderFile);
            
            // 提取PreLoader
            var tmpPath = Path.Combine(PathList.LeviLaminaTempFolder, $"preload_{Guid.NewGuid():N}");
            ZipHelper.ExtractZipFile(preloaderPath, tmpPath);
            var sourceFile = Path.Combine(tmpPath, "bin", "PreLoader.dll");
            
            if (File.Exists(sourceFile))
            {
                File.Move(sourceFile, preloaderFile, true);
            }
            else
            {
                throw new FileNotFoundException("在压缩包中找不到 PreLoader.dll", sourceFile);
            }
            
            // 清理临时文件
            try { Directory.Delete(tmpPath, true); } catch { }
        }
        catch (Exception ex)
        {
            ReportError($"安装PreLoader失败: {ex.Message}");
            throw;
        }
    }

    private async Task DownloadWithRetry(string url, string path, InstallerStatus status, string description, int maxRetries)
    {
        int retryCount = 0;
        
        while (retryCount <= maxRetries)
        {
            try
            {
                Progress?.Report(new()
                {
                    Message = $"{description} (尝试 {retryCount + 1}/{maxRetries + 1})",
                    Status = status,
                    Progress = 0
                });
                
                await new GithubFilesDownloader().DownloadAsync(url, path, 
                    new Progress<DownloadProgress>(p =>
                    {
                        Progress?.Report(new()
                        {
                            Message = $"下载{description}",
                            Status = status,
                            Progress = p.ProgressPercentage
                        });
                    }));
                
                return; // 下载成功，退出循环
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                Console.WriteLine($@"{description} 下载失败，正在重试 ({retryCount}/{maxRetries}): {ex.Message}");
                
                // 等待一段时间后重试
                await Task.Delay(1000 * retryCount);
                
                // 删除可能损坏的文件
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
            catch (Exception ex)
            {
                ReportError($"下载{description}失败: {ex.Message}");
                throw;
            }
        }
    }

    private async Task<Dictionary<DependenciesType, string>> GetDependenceDownloadUrlsAsync(
        ToothManifest.VariantEntry delInfo)
    {
        var notNecessarilyDel = new List<string>()
        {
            "github.com/LiteLDev/levilamina-loc#client",
            "github.com/LiteLDev/PeEditor"
        };

        var result = new Dictionary<DependenciesType, string>();
        var errors = new ConcurrentBag<Exception>();

        var tasks = new List<Task>();

        foreach (var dep in delInfo.Dependencies)
        {
            if (notNecessarilyDel.Contains(dep.Key))
                continue;

            var depParts = dep.Key.Split('/');
            if (depParts.Length < 3)
                continue;

            var orgName = depParts[1];
            var repNameWithSuffix = depParts[2];
            var repName = repNameWithSuffix.Split('#')[0];

            tasks.Add(ProcessDependencyAsync(dep.Value, orgName, repName, result, errors));
        }

        await Task.WhenAll(tasks);

        if (errors.Count > 0)
        {
            ReportError($"解析依赖失败: {string.Join("; ", errors.Select(e => e.Message))}");
            throw new AggregateException("解析依赖时发生错误", errors.ToList());
        }

        return result;
    }

    private async Task ProcessDependencyAsync(
        string depVersion,
        string orgName,
        string repName,
        Dictionary<DependenciesType, string> result,
        ConcurrentBag<Exception> errors)
    {
        try
        {
            var ghClient = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
            var versionPattern = depVersion.Replace("*", "");

            var releases = await ghClient.Repository.Release.GetAll(orgName, repName);
            var matchingRelease = releases.FirstOrDefault(x =>
                x.TagName.Contains(versionPattern) ||
                x.Name?.Contains(versionPattern) == true);

            if (matchingRelease == null)
            {
                throw new Exception($"未找到匹配 {versionPattern} 的发布版本: {repName}");
            }

            if (matchingRelease.Assets.Count == 0)
            {
                throw new Exception($"发布版本没有资源文件: {repName} {matchingRelease.TagName}");
            }

            var depType = repName switch
            {
                "CrashLogger" => DependenciesType.CrashLogger,
                "bedrock-runtime-data" => DependenciesType.BedrockRtd,
                "PreLoader" => DependenciesType.PreLoader,
                _ => (DependenciesType?)null
            };

            if (depType.HasValue)
            {
                lock (result)
                {
                    result[depType.Value] = matchingRelease.Assets[0].BrowserDownloadUrl;
                }
                Console.WriteLine($@"已解析 {repName}: {matchingRelease.TagName}");
            }
        }
        catch (Exception ex)
        {
            errors.Add(new Exception($"处理依赖 {repName} 失败: {ex.Message}", ex));
        }
    }

    private InstallerStatus GetStatusFromDepType(DependenciesType depType)
    {
        return depType switch
        {
            DependenciesType.LeviLamina => InstallerStatus.DownloadLeviLamina,
            DependenciesType.CrashLogger => InstallerStatus.DownloadCrashLogger,
            DependenciesType.BedrockRtd => InstallerStatus.DownloadBedrockRtd,
            DependenciesType.PreLoader => InstallerStatus.DownloadPreLoader,
            _ => InstallerStatus.Error
        };
    }

    private string GetCleanFileNameFromUri(string uriString)
    {
        try
        {
            Uri uri = new Uri(uriString);
            string path = uri.AbsolutePath;
            string fileName = Path.GetFileName(path);

            if (string.IsNullOrEmpty(fileName))
            {
                var segments = uri.Segments.Where(s => !string.IsNullOrWhiteSpace(s));
                if (segments.Any())
                {
                    fileName = segments.Last().TrimEnd('/');
                }
            }

            // 如果还是没有文件名，生成一个基于URL的哈希值
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"file_{Math.Abs(uriString.GetHashCode())}";
            }

            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析文件名失败: {ex.Message}");
            return $"file_{Guid.NewGuid():N}";
        }
    }

    private void ReportError(string message)
    {
        Progress?.Report(new()
        {
            Message = message,
            Progress = 0,
            Status = InstallerStatus.Error
        });
        
        Console.WriteLine($@"错误: {message}");
    }
}