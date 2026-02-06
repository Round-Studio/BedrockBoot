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

namespace BedrockBoot.LeviLamina.Models.Installer;

public class LeviLaminaInstaller
{
    public VersionConfig VersionInfo { get; private set; }
    public IProgress<InstallerProgress> Progress { get; set; }

    public LeviLaminaInstaller(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig;
    }

    public async Task<List<string>> GetVersions()
    {
        var lmaDb = await new LeviLaminaManifestApi().GetVersions();
        var result = new List<string>();
        lmaDb.Versions.Keys.ToList().ForEach(x =>
        {
            if (VersionInfo.Info.Version.Replace(".", "").StartsWith(x))
            {
                result = lmaDb.Versions[x];
            }
        });

        if (result.Count <= 0) throw new NullReferenceException("这个版本不适用于 LeviLamina 喵");

        return result;
    } // 获取符合该版本的 LeviLamina 列表

    public async Task InstallLeviLamina(string lmaVersion)
    {
        Progress.Report(new()
        {
            Message = "获取 LeviLamina 源码...",
            Progress = 0,
            Status = InstallerStatus.DownloadSource
        });
        var sourceUrl = SourceList.LeviLaminaSource.Replace("{version}", lmaVersion);
        var path = Path.Combine(PathList.LeviLaminaSourceFolder, $"{lmaVersion}.zip");

        if (!Path.Exists(PathList.LeviLaminaSourceFolder)) Directory.CreateDirectory(PathList.LeviLaminaSourceFolder);
        if (!Path.Exists(PathList.LeviLaminaCacheFolder)) Directory.CreateDirectory(PathList.LeviLaminaCacheFolder);

        Console.WriteLine(sourceUrl);
        var downloader = new GithubFilesDownload();
        await downloader.DownloadAsync(sourceUrl, path, new Progress<DownloadProgress>(p =>
        {
            Progress.Report(new()
            {
                Message = $"下载 LeviLamina 源码 ({p.BytesPerSecond} / s)",
                Progress = p.ProgressPercentage,
                Status = InstallerStatus.DownloadSource
            });
        }));

        if (!Path.Exists(PathList.LeviLaminaTempFolder)) Directory.CreateDirectory(PathList.LeviLaminaTempFolder);
        var tmpFolder = Path.Combine(PathList.LeviLaminaTempFolder, $"ll_{lmaVersion}");
        ZipHelper.ExtractZipFile(path, tmpFolder);

        var llJson = Path.Combine(tmpFolder, $"LeviLamina-{lmaVersion}", "tooth.json");
        var llManifest = new ConfigEntity<ToothManifest>(llJson, false);
        var depInfo = llManifest.Data.Variants[1];
        if (string.IsNullOrEmpty(depInfo.Label)) throw new NullReferenceException("非客户端文件");
        if (depInfo.Label != "client") throw new NullReferenceException("非客户端文件");

        var allUrls = depInfo.Assets
            .SelectMany(asset => asset.Urls
                .Select(url => url
                    .Replace("{{tooth}}", llManifest.Data.Tooth)
                    .Replace("{{version}}", llManifest.Data.Version)))
            .ToList();

        allUrls.ForEach(Console.WriteLine);
        var deps = await GetDependenceDownloadUrlsAsync(depInfo);
        deps.ToList().ForEach((d) => Console.WriteLine($"{d.Key} {d.Value}"));
        allUrls.ForEach(x => deps.Add(DependenciesType.LeviLamina, x));

        var tasks = new List<Task>();
        deps.ToList().ForEach(dep =>
        {
            var depPath = Path.Combine(PathList.LeviLaminaCacheFolder, GetCleanFileNameFromUri(dep.Value));

            // 创建包含后续操作的任务
            var task = Task.Run(async () =>
            {
                try
                {
                    // 下载文件
                    await new GithubFilesDownload().DownloadAsync(dep.Value, depPath,
                        new Progress<DownloadProgress>(p =>
                        {
                            Progress.Report(new()
                            {
                                Message = "下载组件",
                                Status = dep.Key switch
                                {
                                    DependenciesType.LeviLamina => InstallerStatus.DownloadLeviLamina,
                                    DependenciesType.CrashLogger => InstallerStatus.DownloadCrashLogger,
                                    DependenciesType.BedrockRtd => InstallerStatus.DownloadBedrockRtd,
                                    DependenciesType.PreLoader => InstallerStatus.DownloadPreLoader
                                },
                                Progress = p.ProgressPercentage
                            });
                        }));

                    Console.WriteLine($"{dep.Key} 下载完成: {Path.GetFileName(depPath)}");

                    if (!Directory.Exists(Path.Combine(VersionInfo.VersionPath, "mods", "LeviLamina")))
                        Directory.CreateDirectory(Path.Combine(VersionInfo.VersionPath, "mods", "LeviLamina"));

                    // 根据不同类型执行不同操作
                    switch (dep.Key)
                    {
                        case DependenciesType.LeviLamina:
                            ZipHelper.ExtractZipFile(depPath,
                                Path.Combine(VersionInfo.VersionPath, "mods"),true);
                            // LeviLamina 特定操作
                            Console.WriteLine("LeviLamina 安装完成");
                            break;

                        case DependenciesType.CrashLogger:
                            ZipHelper.ExtractZipFile(depPath,
                                Path.Combine(VersionInfo.VersionPath, "mods", "LeviLamina"),true);
                            // CrashLogger 特定操作
                            Console.WriteLine("CrashLogger 安装完成");
                            break;

                        case DependenciesType.BedrockRtd:
                            ZipHelper.ExtractZipFile(depPath, VersionInfo.VersionPath, true);
                            // BedrockRTD 特定操作
                            Console.WriteLine("BedrockRTD 安装完成");
                            break;

                        case DependenciesType.PreLoader:
                            var file = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods",
                                "PreLoader.dll");
                            if(File.Exists(file)) File.Delete(file);
                            var conf = new ConfigEntity<List<ModInfo>>(Path.Combine(VersionInfo.VersionPath, "config",
                                "BedrockBoot2", "mods.json"));
                            conf.Data.Add(new ModInfo()
                            {
                                File = file,
                                IsPreLoad = true,
                                InjectDelay = 0
                            });
                            conf.Save();

                            var tmpPath = Path.Combine(PathList.LeviLaminaTempFolder,
                                $"preload_{Guid.NewGuid().ToString().Replace("-", "")}");
                            ZipHelper.ExtractZipFile(depPath, tmpPath);
                            var bodyFile = Path.Combine(tmpPath, "bin", "PreLoader.dll");
                            File.Move(bodyFile, file);
                            
                            // PreLoader 特定操作
                            Console.WriteLine("PreLoader 安装完成");
                            break;
                    }

                    // 更新进度
                    Progress.Report(new()
                    {
                        Message = $"{dep.Key} 处理完成",
                        Status = InstallerStatus.Processed,
                        Progress = 100
                    });

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{dep.Key} 处理失败: {ex.Message}");
                    throw;
                }
            });

            tasks.Add(task);
        });

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine("所有组件下载并处理完成");
        Progress.Report(new()
        {
            Message = "准备下一步安装",
            Status = InstallerStatus.Complete,
            Progress = 100
        });
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

        // 并行处理所有依赖，提高效率
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

            // 创建任务处理每个依赖
            tasks.Add(ProcessDependencyAsync(dep.Value, orgName, repName, result));
        }

        // 等待所有任务完成
        await Task.WhenAll(tasks);

        return result;
    }

    private async Task ProcessDependencyAsync(
        string depVersion,
        string orgName,
        string repName,
        Dictionary<DependenciesType, string> result)
    {
        try
        {
            var ghClient = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

            // 获取所有发布
            var releases = await ghClient.Repository.Release.GetAll(orgName, repName);

            // 构建版本匹配模式
            var versionPattern = depVersion.Replace("*", "");
            var matchingRelease = releases.FirstOrDefault(x =>
                x.TagName.Contains(versionPattern) ||
                x.Name?.Contains(versionPattern) == true);

            if (matchingRelease == null)
            {
                Console.WriteLine($"未找到匹配 {versionPattern} 的发布版本");
                return;
            }

            if (matchingRelease.Assets.Count == 0)
            {
                Console.WriteLine($"发布版本 {matchingRelease.TagName} 没有资源文件");
                return;
            }

            // 根据仓库名称确定依赖类型
            DependenciesType? depType = repName switch
            {
                "CrashLogger" => DependenciesType.CrashLogger,
                "PreLoader" => DependenciesType.PreLoader,
                "bedrock-runtime-data" => DependenciesType.BedrockRtd,
                _ => null
            };

            if (depType.HasValue)
            {
                lock (result) // 确保线程安全
                {
                    result[depType.Value] = matchingRelease.Assets[0].BrowserDownloadUrl;
                }

                Console.WriteLine($"已解析 {repName}: {matchingRelease.TagName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理依赖 {repName} 失败: {ex.Message}");
        }
    }
    private string GetCleanFileNameFromUri(string uriString)
    {
        Uri uri = new Uri(uriString);
    
        // 获取路径部分
        string path = uri.AbsolutePath;
    
        // 获取文件名（包含扩展名）
        string fileName = Path.GetFileName(path);
    
        // 如果没有文件名（例如以斜杠结尾）
        if (string.IsNullOrEmpty(fileName))
        {
            // 尝试从最后一个非空段获取
            var segments = uri.Segments.Where(s => !string.IsNullOrWhiteSpace(s));
            if (segments.Any())
            {
                fileName = segments.Last().TrimEnd('/');
            }
        }
    
        return fileName;
    }
}