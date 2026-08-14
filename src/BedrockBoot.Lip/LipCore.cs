using Octokit;
using System.Text.Json;
using BedrockBoot.Lip.Manifest;
using BedrockBoot.Base.Entry.Progress;
using Octokit.Internal;
using System.IO.Compression;
using BedrockBoot.Downloader.Files;
using BedrockBoot.Models.Global;
using BedrockBoot.Lip.Global;

namespace BedrockBoot.Lip;

public class LipCore
{
    private readonly string _owner;
    private readonly string _repo;
    private string _version;
    private readonly string _label;
    private readonly string _tooth;
    private List<string>? _tags = null;
    private readonly HttpClient _httpClient = new HttpClient();
    private GitHubClient? _client;
    private string _installFolder;
    private static readonly Dictionary<string, List<string>> _tagCache = new Dictionary<string, List<string>>();
    private static readonly object _cacheLock = new object();
    private static string? _githubToken = null;
    private static readonly PathMappings _pathMappings = new PathMappings();

    public static void SetGitHubToken(string token)
    {
        _githubToken = token;
    }

    public LipCore(string owner, string repo, string version, string label = "client", string host = "github.com")
    {
        _owner = owner;
        _repo = repo;
        _version = version;
        _label = label;
        _tooth = $"{host}/{owner}/{repo}";
        InitializeGitHubClient();
    }

    public LipCore(string toothStr)
    {
        var replist = toothStr.Split('/');
        _owner = replist[1];
        var versionPart = replist[2].Split('@')[1];
        _version = versionPart;
        _repo = replist[2].Split('@')[0].Split('#')[0];
        _label = replist[2].Split('@')[0].Split('#')[1];
        _tooth = $"{replist[0]}/{_owner}/{_repo}";
        InitializeGitHubClient();
    }

    private void InitializeGitHubClient()
    {
        var connection = new Connection(
            new ProductHeaderValue("BedrockBoot.LipClient"),
            new Uri("https://api.github.com"),
            new InMemoryCredentialStore(
                string.IsNullOrEmpty(_githubToken)
                    ? Credentials.Anonymous
                    : new Credentials(_githubToken)
            )
        );
        _client = new GitHubClient(connection);
    }

    public async Task<List<string>?> GetAllTags()
    {
        if (_tags != null) return _tags;

        var cacheKey = $"{_owner}/{_repo}";
        lock (_cacheLock)
        {
            if (_tagCache.TryGetValue(cacheKey, out var cachedTags))
            {
                _tags = cachedTags;
                return _tags;
            }
        }

        if (_client == null)
            InitializeGitHubClient();

        try
        {
            IReadOnlyList<RepositoryTag> tags = await _client.Repository.GetAllTags(_owner, _repo);
            _tags = tags.Select(t => t.Name).ToList();

            lock (_cacheLock)
            {
                _tagCache[cacheKey] = _tags;
            }

            return _tags;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"发生错误: {ex.Message}");
            throw;
        }
    }

    private string FindMatchingTag(List<string> tags, string version)
    {
        var pattern = version.Replace("*", "").Replace("v", "");
        var matched = tags.Where(x => x.Replace("v", "").Contains(pattern)).ToList();
        return matched.Count > 0 ? matched[0] : null;
    }

    private string GetCachePath(string fileName)
    {
        return Path.Combine(PathsList.TempPath, "lip_cache", fileName);
    }

    private string GetExtractPath(string fileName)
    {
        var extractDir = Path.Combine(PathsList.TempPath, "lip_extract", Path.GetFileNameWithoutExtension(fileName));
        return extractDir;
    }

    private string MapDestinationPath(string dest)
    {
        var result = dest;
        foreach (var mapping in _pathMappings.PathMappingsList)
        {
            if (dest.StartsWith(mapping.src))
            {
                result = dest.Replace(mapping.src, mapping.dest);
                Console.WriteLine($@"路径映射: {dest} -> {result}");
                break;
            }
        }
        return result;
    }

    private bool ShouldSkipDependency(string tooth)
    {
        foreach (var skipTooth in _pathMappings.DontInstallDeepsList)
        {
            if (tooth.Contains(skipTooth))
            {
                Console.WriteLine($@"跳过依赖: {tooth}");
                return true;
            }
        }
        return false;
    }

    public async Task Install(string installFolder, IProgress<DownloadProgress>? progressCallback = null)
    {
        _installFolder = installFolder;
        var tags = await GetAllTags();
        var tagName = FindMatchingTag(tags, _version);
        _version = tagName.Replace("v", "");

        if (string.IsNullOrEmpty(tagName))
        {
            Console.WriteLine($@"可用tags: {string.Join(", ", tags.Take(10))}...");
            throw new Exception($"找不到匹配的版本: {_version}");
        }

        if (_client == null)
            InitializeGitHubClient();

        var file = await _client.Repository.Content.GetRawContentByRef(_owner, _repo, "tooth.json", tagName);
        var jsonContent = System.Text.Encoding.UTF8.GetString(file);

        var manifest = JsonSerializer.Deserialize<ToothFile>(jsonContent);
        if (manifest == null)
            throw new Exception("无法解析 tooth.json");

        var variant = manifest.Variants?.FindLast(v =>
        {
            if (v?.Label != null) return v.Label == _label;
            return false;
        });

        if (variant == null && manifest.Variants != null && manifest.Variants.Count > 0)
            variant = manifest.Variants[0];

        if (variant == null)
            throw new Exception("没有找到合适的 variant");

        Console.WriteLine($@"正在安装 {_tooth}@{_version} (label: {_label})");

        await InstallDependencies(variant.Dependencies, progressCallback);

        if (variant.Assets == null || variant.Assets.Count == 0)
        {
            Console.WriteLine($@"没有需要下载的资源: {_tooth}");
            return;
        }

        var downloadTasks = new List<Task>();
        var totalAssets = variant.Assets.Count;
        var completedAssets = 0;
        var overallProgress = new DownloadProgress();

        foreach (var asset in variant.Assets)
        {
            if (asset == null)
            {
                Interlocked.Increment(ref completedAssets);
                continue;
            }

            if (asset.Type == "self")
            {
                await ProcessSelfAsset(asset, tagName);
                Interlocked.Increment(ref completedAssets);
                continue;
            }

            if (asset.Urls == null || asset.Urls.Count == 0)
            {
                Console.WriteLine($@"Asset 类型 {asset.Type} 没有 URLs，跳过");
                Interlocked.Increment(ref completedAssets);
                continue;
            }

            var assetUrls = asset.Urls.Select(url =>
                url.Replace("{{tooth}}", _tooth)
                    .Replace("{{version}}", _version)
            ).ToList();

            foreach (var url in assetUrls)
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                var cachePath = GetCachePath(fileName);
                var extractPath = GetExtractPath(fileName);
                var currentAssetIndex = completedAssets;

                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

                if (File.Exists(cachePath))
                {
                    Console.WriteLine($@"缓存文件已存在: {fileName}");
                    await ProcessZipAsset(asset, cachePath, extractPath);
                    Interlocked.Increment(ref completedAssets);
                    continue;
                }

                var downloader = new GithubFilesDownloader();

                var task = downloader.DownloadAsync(
                    url,
                    cachePath,
                    progressCallback != null
                        ? new Progress<DownloadProgress>(p =>
                        {
                            overallProgress.TotalBytes = p.TotalBytes;
                            overallProgress.DownloadedBytes = p.DownloadedBytes;
                            overallProgress.BytesPerSecond = p.BytesPerSecond;
                            overallProgress.EstimatedRemainingSeconds = p.EstimatedRemainingSeconds;
                            overallProgress.Message = $"[{currentAssetIndex + 1}/{totalAssets}] {p.Message} {url}";
                            progressCallback.Report(overallProgress);
                        })
                        : null
                ).ContinueWith(async downloadTask =>
                {
                    if (downloadTask.Result)
                    {
                        Console.WriteLine($@"下载完成: {fileName}");
                        await ProcessZipAsset(asset, cachePath, extractPath);
                    }
                    else
                    {
                        Console.WriteLine($@"下载失败: {fileName}");
                    }

                    Interlocked.Increment(ref completedAssets);
                });

                downloadTasks.Add(task);
                await Task.Delay(100);
            }
        }

        await Task.WhenAll(downloadTasks);
        Console.WriteLine($@"安装完成: {_tooth}@{_version}");
    }

    private async Task ProcessZipAsset(Asset asset, string zipPath, string extractPath)
    {
        if (asset.Placements == null || asset.Placements.Count == 0)
        {
            Console.WriteLine($@"ZIP 文件没有 placements 配置: {zipPath}");
            return;
        }

        if (Directory.Exists(extractPath))
        {
            Console.WriteLine($@"已解压: {extractPath}");
            await ApplyPlacements(asset, extractPath);
            return;
        }

        Directory.CreateDirectory(extractPath);

        try
        {
            Console.WriteLine($@"解压 ZIP: {zipPath}");
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            Console.WriteLine($@"解压完成: {extractPath}");

            await ApplyPlacements(asset, extractPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"处理 ZIP 失败: {ex.Message}");
            throw;
        }
    }

    private async Task ApplyPlacements(Asset asset, string extractPath)
    {
        foreach (var placement in asset.Placements)
        {
            if (placement == null)
                continue;

            // 映射目标路径
            var mappedDest = MapDestinationPath(placement.Dest.TrimStart('/').TrimEnd('/'));

            if (placement.Type == "file")
            {
                var srcFile = Path.Combine(extractPath, placement.Src.TrimStart('/').TrimEnd('/'));
                var destFile = Path.Combine(_installFolder, mappedDest);

                if (!File.Exists(srcFile))
                {
                    Console.WriteLine($@"源文件不存在: {srcFile}");
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(srcFile, destFile, true);
                Console.WriteLine($@"已复制文件: {placement.Src} -> {destFile}");
            }
            else if (placement.Type == "dir")
            {
                var srcPath = Path.Combine(extractPath, placement.Src.TrimStart('/').TrimEnd('/'));
                var destPath = Path.Combine(_installFolder, mappedDest);

                if (!Directory.Exists(srcPath))
                {
                    Console.WriteLine($@"源目录不存在: {srcPath}");
                    continue;
                }

                Directory.CreateDirectory(destPath);

                foreach (var file in Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(srcPath, file);
                    var targetFile = Path.Combine(destPath, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                    File.Copy(file, targetFile, true);
                    Console.WriteLine($@"已复制目录: {relativePath} -> {targetFile}");
                }
            }
            else
            {
                Console.WriteLine($@"未知的 placement 类型: {placement.Type}");
            }
        }
    }

    private async Task ProcessSelfAsset(Asset asset, string tagName)
    {
        if (asset.Placements == null || asset.Placements.Count == 0)
            return;

        foreach (var placement in asset.Placements)
        {
            if (placement == null)
                continue;

            // 映射目标路径
            var mappedDest = MapDestinationPath(placement.Dest.TrimStart('/').TrimEnd('/'));

            if (placement.Type == "file")
            {
                try
                {
                    var fileContent = await _client.Repository.Content.GetRawContentByRef(
                        _owner,
                        _repo,
                        placement.Src.TrimStart('/').TrimEnd('/'),
                        tagName
                    );

                    var destPath = Path.Combine(
                        _installFolder,
                        mappedDest
                    );

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    await File.WriteAllBytesAsync(destPath, fileContent);
                    Console.WriteLine($@"已复制文件: {placement.Src} -> {destPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"处理 self asset (file) 失败: {ex.Message}");
                    throw;
                }
            }
            else if (placement.Type == "dir")
            {
                try
                {
                    var contents = await _client.Repository.Content.GetAllContentsByRef(
                        _owner,
                        _repo,
                        placement.Src.TrimEnd('/'),
                        tagName
                    );

                    foreach (var content in contents)
                    {
                        if (content.Type == ContentType.File)
                        {
                            var fileContent = await _client.Repository.Content.GetRawContentByRef(
                                _owner,
                                _repo,
                                content.Path,
                                tagName
                            );

                            var destPath = Path.Combine(
                                _installFolder,
                                mappedDest,
                                content.Name
                            );

                            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                            await File.WriteAllBytesAsync(destPath, fileContent);
                            Console.WriteLine($@"已复制: {content.Path} -> {destPath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"处理 self asset (dir) 失败: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine($@"未知的 placement 类型: {placement.Type}");
            }
        }
    }

    private async Task InstallDependencies(Dictionary<string, string> dependencies,
        IProgress<DownloadProgress>? progressCallback = null)
    {
        if (dependencies == null || dependencies.Count == 0)
            return;

        Console.WriteLine($@"开始安装 {dependencies.Count} 个依赖...");

        var dependencyTasks = dependencies
            .Where(dep => !ShouldSkipDependency(dep.Key))
            .Select(dep =>
        {
            var toothStr = dep.Key;
            var versionRange = dep.Value;

            if (!toothStr.Contains('#'))
            {
                toothStr += $"#{_label}";
            }

            var depTooth = $"{toothStr}@{versionRange}";

            return Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($@"开始安装依赖: {dep.Key} ({versionRange})");
                    var depCore = new LipCore(depTooth);
                    await depCore.Install(_installFolder, progressCallback);
                    Console.WriteLine($@"依赖 {dep.Key} 安装完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"安装依赖 {dep.Key} 失败: {ex.Message}");
                    throw;
                }
            });
        }).ToList();

        if (dependencyTasks.Count == 0)
        {
            Console.WriteLine(@"所有依赖都已跳过");
            return;
        }

        await Task.WhenAll(dependencyTasks);
        Console.WriteLine(@"所有依赖安装完成");
    }
}