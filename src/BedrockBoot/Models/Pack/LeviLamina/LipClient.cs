using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Core.Models.Download;

namespace BedrockBoot.Models.Pack.LeviLamina
{
    public class LipClient
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly GithubFilesDownloader _downloader;
        private string installDir;
        private bool isClientMode = false;
        private string version;
        private string tooth;
        private string toothPath;
        private HashSet<string> installedDependencies = new HashSet<string>();

        public int TotalProgress { get; private set; }
        public event EventHandler<int> ProgressChanged;
        public string InstallDirectory { get; set; } = Environment.CurrentDirectory;

        public LipClient()
        {
            TotalProgress = 0;
            _downloader = new GithubFilesDownloader();
        }

        public LipClient(string installDirectory)
        {
            TotalProgress = 0;
            InstallDirectory = installDirectory;
            _downloader = new GithubFilesDownloader();
        }

        public async Task InstallAsync(string installString)
        {
            try
            {
                ParseInstallString(installString);

                installDir = InstallDirectory;

                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                Console.WriteLine($@"安装目标: {tooth}");
                Console.WriteLine($@"版本: {version}");
                Console.WriteLine($@"模式: {(isClientMode ? "客户端" : "服务端")}");
                Console.WriteLine($@"安装目录: {installDir}");

                UpdateProgress(5);

                var config = await LoadConfigAsync();
                UpdateProgress(15);

                var variant = SelectVariant(config);
                UpdateProgress(20);

                await DownloadDependenciesAsync(variant.Dependencies, tooth);
                UpdateProgress(50);

                await DownloadAndExtractMainAssetAsync(config, variant);
                UpdateProgress(80);

                if (variant?.Scripts != null) await ExecuteScriptsAsync(variant.Scripts.PostInstall);
                UpdateProgress(95);

                Console.WriteLine();
                Console.WriteLine(@"安装完成");
                UpdateProgress(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"安装失败: {ex.Message}");
                throw;
            }
        }

        private void ParseInstallString(string installString)
        {
            if (string.IsNullOrEmpty(installString))
                throw new ArgumentException("安装字符串不能为空");

            string toothPart = installString;
            string versionPart = null;
            string labelPart = null;

            int atIndex = installString.LastIndexOf('@');
            if (atIndex > 0)
            {
                versionPart = installString.Substring(atIndex + 1);
                toothPart = installString.Substring(0, atIndex);
            }
            else
            {
                throw new ArgumentException($"安装字符串缺少版本号: {installString}");
            }

            int hashIndex = toothPart.LastIndexOf('#');
            if (hashIndex > 0)
            {
                labelPart = toothPart.Substring(hashIndex + 1);
                toothPart = toothPart.Substring(0, hashIndex);
            }

            if (string.IsNullOrEmpty(toothPart))
                throw new ArgumentException($"安装字符串缺少仓库地址: {installString}");

            if (string.IsNullOrEmpty(versionPart))
                throw new ArgumentException($"安装字符串缺少版本号: {installString}");

            tooth = toothPart;
            version = versionPart;

            if (!string.IsNullOrEmpty(labelPart) && labelPart == "client")
                isClientMode = true;

            if (tooth.StartsWith("github.com/"))
                toothPath = tooth.Substring(11);
            else
                toothPath = tooth;

            Console.WriteLine($@"解析结果: tooth={tooth}, version={version}, client={isClientMode}");
        }

        private void UpdateProgress(int progress)
        {
            TotalProgress = Math.Min(100, Math.Max(0, progress));
            ProgressChanged?.Invoke(this, TotalProgress);
        }

        private async Task<LeviConfig> LoadConfigAsync()
        {
            string configUrl = $"https://fastly.jsdelivr.net/gh/{toothPath}@v{version}/tooth.json";

            Console.WriteLine($@"加载配置: {configUrl}");

            try
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("LipClient");
                string configJson = await httpClient.GetStringAsync(configUrl);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var config = JsonSerializer.Deserialize<LeviConfig>(configJson, options);

                if (config == null)
                    throw new Exception("配置解析失败");

                Console.WriteLine($@"配置加载成功: {config.Info?.Name ?? "未知"} v{config.Version}");
                return config;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"无法获取配置文件 (版本 {version}): {ex.Message}");
            }
        }

        private Variant SelectVariant(LeviConfig config)
        {
            if (isClientMode)
            {
                var clientVariant = config.Variants.FirstOrDefault(v => v.Label == "client");
                if (clientVariant != null)
                {
                    Console.WriteLine(@"使用客户端变体");
                    return clientVariant;
                }

                Console.WriteLine(@"未找到客户端变体，使用默认变体");
            }

            if (config.Variants.Count > 0)
            {
                Console.WriteLine(
                    $@"使用变体索引 0 {(config.Variants[0].Label != null ? $"({config.Variants[0].Label})" : "")}");
                return config.Variants[0];
            }

            throw new Exception("未找到可用的安装变体");
        }

        private async Task DownloadDependenciesAsync(Dictionary<string, string> dependencies, string mainTooth)
        {
            if (dependencies == null || dependencies.Count == 0)
            {
                Console.WriteLine(@"无依赖需要安装");
                return;
            }

            Console.WriteLine($@"安装依赖 ({dependencies.Count} 个)");

            int depIndex = 0;
            foreach (var dep in dependencies)
            {
                try
                {
                    string depName = dep.Key;
                    string depVersionSpec = dep.Value;

                    if (depName == mainTooth || depName == mainTooth + "#client")
                    {
                        Console.WriteLine($@"跳过自身依赖: {depName} ({depVersionSpec})");
                        depIndex++;
                        continue;
                    }

                    string depLabel = "";
                    if (isClientMode && depName.Contains("#client"))
                    {
                        depLabel = "#client";
                        depName = depName.Replace("#client", "");
                    }

                    Console.WriteLine($@"安装依赖 [{depIndex + 1}/{dependencies.Count}]: {depName} ({depVersionSpec})");

                    if (depName == "github.com/LiteLDev/LeviLamina")
                    {
                        Console.WriteLine(@"依赖为 LeviLamina 本体，跳过安装");
                        continue;
                    }

                    string resolvedVersion = await ResolveDependencyVersionAsync(depName, depVersionSpec);

                    if (string.IsNullOrEmpty(resolvedVersion))
                    {
                        Console.WriteLine($@"警告: 无法解析版本 {depVersionSpec}，跳过依赖 {depName}");
                        depIndex++;
                        continue;
                    }

                    string depKey = $"{depName}@{resolvedVersion}";
                    if (installedDependencies.Contains(depKey))
                    {
                        Console.WriteLine($@"依赖已安装: {depKey}");
                        depIndex++;
                        continue;
                    }

                    string depInstallString = $"{depName}{depLabel}@{resolvedVersion}";

                    var depInstaller = new LipClient(installDir);
                    depInstaller.ProgressChanged += (s, p) =>
                    {
                        int baseProgress = 20;
                        int rangeProgress = 30;
                        int depProgress = baseProgress + (depIndex * rangeProgress / dependencies.Count) +
                                          (p * rangeProgress / 100 / dependencies.Count);
                        UpdateProgress(depProgress);
                    };

                    await depInstaller.InstallAsync(depInstallString);
                    installedDependencies.Add(depKey);
                    depIndex++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"依赖安装失败: {ex.Message}");
                    depIndex++;
                }
            }
        }

        private async Task<string> ResolveDependencyVersionAsync(string depName, string versionSpec)
        {
            if (!versionSpec.Contains('*'))
                return versionSpec;

            Console.WriteLine($@"解析版本: {versionSpec}");

            string pattern = "^" + Regex.Escape(versionSpec).Replace("\\*", ".*") + "$";
            var regex = new Regex(pattern);

            string repoPath = depName.Replace("github.com/", "");

            try
            {
                string tagsUrl = $"https://api.github.com/repos/{repoPath}/tags?per_page=100";
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("LipClient");

                var response = await httpClient.GetAsync(tagsUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var tags = JsonSerializer.Deserialize<List<GitHubTag>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (tags != null && tags.Count > 0)
                    {
                        var matchedVersions = tags
                            .Where(t => t.Name != null && regex.IsMatch(t.Name))
                            .Select(t => t.Name)
                            .ToList();

                        if (matchedVersions.Count > 0)
                        {
                            string latest = GetLatestVersion(matchedVersions);
                            Console.WriteLine($@"匹配到版本: {latest}");
                            return latest;
                        }
                    }
                }

                string releasesUrl = $"https://api.github.com/repos/{repoPath}/releases?per_page=100";
                response = await httpClient.GetAsync(releasesUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (releases != null && releases.Count > 0)
                    {
                        var matchedVersions = releases
                            .Where(r => r.TagName != null && regex.IsMatch(r.TagName))
                            .Select(r => r.TagName)
                            .ToList();

                        if (matchedVersions.Count > 0)
                        {
                            string latest = GetLatestVersion(matchedVersions);
                            Console.WriteLine($@"匹配到版本: {latest}");
                            return latest;
                        }
                    }
                }

                Console.WriteLine($@"未找到匹配版本: {versionSpec}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"版本解析失败: {ex.Message}");
                return null;
            }
        }

        private string GetLatestVersion(List<string> versions)
        {
            var sorted = versions
                .Select(v => new { Version = v, Parts = ParseVersion(v) })
                .Where(v => v.Parts != null)
                .OrderByDescending(v => v.Parts[0])
                .ThenByDescending(v => v.Parts.Length > 1 ? v.Parts[1] : 0)
                .ThenByDescending(v => v.Parts.Length > 2 ? v.Parts[2] : 0)
                .ToList();

            if (sorted.Count == 0)
                return versions.OrderByDescending(v => v).First();

            var stable = sorted.Where(v => !v.Version.Contains('-')).ToList();
            if (stable.Count > 0)
                return stable.First().Version;
            else
                return sorted.First().Version;
        }

        private int[] ParseVersion(string version)
        {
            try
            {
                string clean = version.TrimStart('v');
                int dashIndex = clean.IndexOf('-');
                if (dashIndex > 0)
                    clean = clean.Substring(0, dashIndex);

                int plusIndex = clean.IndexOf('+');
                if (plusIndex > 0)
                    clean = clean.Substring(0, plusIndex);

                var parts = clean.Split('.').Select(int.Parse).ToArray();
                return parts;
            }
            catch
            {
                return null;
            }
        }

        private async Task DownloadAndExtractMainAssetAsync(LeviConfig config, Variant variant)
        {
            var asset = variant.Assets.FirstOrDefault();
            if (asset == null)
                throw new Exception("未找到可用的资源文件");

            var url = asset.Urls.First()
                .Replace("{{tooth}}", tooth)
                .Replace("{{version}}", version);

            Console.WriteLine($@"下载主程序: {url}");

            string fileName = $"temp-{Guid.NewGuid():N}.zip";
            string filePath = Path.Combine(installDir, fileName);

            try
            {
                // 使用 GithubFilesDownloader 下载
                var progress = new Progress<BedrockBoot.Base.Entry.Progress.DownloadProgress>();
                progress.ProgressChanged += (s, p) =>
                {
                    int progressPercent = (int)(p.ProgressPercentage);
                    UpdateProgress(50 + (progressPercent * 30 / 100));
                };

                bool success = await _downloader.DownloadAsync(
                    url,
                    filePath,
                    progress,
                    CancellationToken.None);

                if (!success)
                    throw new Exception("下载失败");

                Console.WriteLine($@"下载完成");

                await ExtractAndPlaceFilesAsync(filePath, variant);

                File.Delete(filePath);
                Console.WriteLine(@"清理临时文件");
            }
            catch (Exception ex)
            {
                throw new Exception($"下载或解压失败: {ex.Message}");
            }
        }

        private async Task ExtractAndPlaceFilesAsync(string zipPath, Variant variant)
        {
            Console.WriteLine(@"解压并放置文件");

            string tempExtractDir = Path.Combine(installDir, $"temp_extract_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempExtractDir);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, tempExtractDir);
                Console.WriteLine($@"解压到临时目录: {tempExtractDir}");

                foreach (var placement in variant.Assets.First().Placements)
                {
                    string srcPath = Path.Combine(tempExtractDir, placement.Src);
                    string destPath = Path.Combine(installDir, placement.Dest);

                    Console.WriteLine($@"处理: {placement.Src} -> {placement.Dest}");

                    if (placement.Type == "dir")
                    {
                        if (Directory.Exists(srcPath))
                        {
                            Directory.CreateDirectory(destPath);
                            CopyDirectory(srcPath, destPath);
                            Console.WriteLine($@"已复制目录: {destPath}");
                        }
                        else
                        {
                            Console.WriteLine($@"警告: 源目录不存在 {srcPath}");
                        }
                    }
                    else if (placement.Type == "file")
                    {
                        if (File.Exists(srcPath))
                        {
                            string destFile = Path.Combine(destPath, Path.GetFileName(srcPath));
                            Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? destPath);
                            File.Copy(srcPath, destFile, true);
                            Console.WriteLine($@"已复制文件: {destFile}");
                        }
                        else
                        {
                            Console.WriteLine($@"警告: 源文件不存在 {srcPath}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($@"警告: 未知的 placement 类型: {placement.Type}");
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempExtractDir))
                {
                    try
                    {
                        Directory.Delete(tempExtractDir, true);
                        Console.WriteLine($@"清理临时目录: {tempExtractDir}");
                    }
                    catch
                    {
                        // 忽略清理错误
                    }
                }
            }

            if (variant.RemoveFiles != null)
            {
                foreach (var file in variant.RemoveFiles)
                {
                    string filePath = Path.Combine(installDir, file);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Console.WriteLine($@"删除: {file}");
                    }
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destDir, fileName);
                File.Copy(filePath, destFilePath, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        private async Task ExecuteScriptsAsync(List<string> scripts)
        {
            if (scripts == null || scripts.Count == 0)
            {
                Console.WriteLine(@"无需执行脚本");
                return;
            }

            Console.WriteLine(@"执行脚本");

            foreach (var script in scripts)
            {
                try
                {
                    await ExecuteSingleScriptAsync(script);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"脚本执行失败: {script}");
                    Console.WriteLine($@"错误: {ex.Message}");
                }
            }
        }

        private async Task ExecuteSingleScriptAsync(string script)
        {
            if (script.Contains("IF EXIST") || script.Contains("IF NOT EXIST"))
            {
                await ExecuteBatchScriptAsync(script);
                return;
            }

            var parts = script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            string command = parts[0];
            string arguments = string.Join(" ", parts.Skip(1));

            if (command.StartsWith(".\\"))
            {
                string exePath = Path.Combine(installDir, command.Substring(2));
                if (!File.Exists(exePath))
                {
                    Console.WriteLine($@"警告: 找不到 {exePath}");
                    return;
                }

                command = exePath;
            }

            Console.WriteLine($@"执行: {script}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = installDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                    throw new Exception("无法启动进程");

                await process.WaitForExitAsync();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($@"输出: {output.Trim()}");

                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($@"错误: {error.Trim()}");

                if (process.ExitCode != 0)
                    Console.WriteLine($@"警告: 退出代码 {process.ExitCode}");
            }
        }

        private async Task ExecuteBatchScriptAsync(string script)
        {
            string batchFile = Path.Combine(installDir, $"temp_install_{Guid.NewGuid():N}.bat");

            try
            {
                string batchContent = script.Replace(".\\", $"{installDir}\\");
                await File.WriteAllTextAsync(batchFile, batchContent);

                Console.WriteLine($@"执行批处理: {script}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFile}\"",
                    WorkingDirectory = installDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                        throw new Exception("无法启动进程");

                    await process.WaitForExitAsync();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine($@"输出: {output.Trim()}");

                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($@"错误: {error.Trim()}");
                }
            }
            finally
            {
                if (File.Exists(batchFile))
                    File.Delete(batchFile);
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    // GitHub API 响应类
    public class GitHubTag
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("zipball_url")] public string ZipballUrl { get; set; }

        [JsonPropertyName("tarball_url")] public string TarballUrl { get; set; }

        [JsonPropertyName("commit")] public GitHubCommit Commit { get; set; }
    }

    public class GitHubCommit
    {
        [JsonPropertyName("sha")] public string Sha { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class GitHubRelease
    {
        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; }

        [JsonPropertyName("assets_url")] public string AssetsUrl { get; set; }

        [JsonPropertyName("upload_url")] public string UploadUrl { get; set; }

        [JsonPropertyName("tarball_url")] public string TarballUrl { get; set; }

        [JsonPropertyName("zipball_url")] public string ZipballUrl { get; set; }

        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("tag_name")] public string TagName { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("body")] public string Body { get; set; }

        [JsonPropertyName("draft")] public bool Draft { get; set; }

        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }

        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

        [JsonPropertyName("published_at")] public DateTime PublishedAt { get; set; }
    }

    // JSON配置类
    public class LeviConfig
    {
        [JsonPropertyName("format_version")] public int FormatVersion { get; set; }

        [JsonPropertyName("format_uuid")] public string FormatUuid { get; set; }

        [JsonPropertyName("tooth")] public string Tooth { get; set; }

        [JsonPropertyName("version")] public string Version { get; set; }

        [JsonPropertyName("info")] public Info Info { get; set; }

        [JsonPropertyName("variants")] public List<Variant> Variants { get; set; }
    }

    public class Info
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("tags")] public List<string> Tags { get; set; }

        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
    }

    public class Variant
    {
        [JsonPropertyName("label")] public string Label { get; set; }

        [JsonPropertyName("platform")] public string Platform { get; set; }

        [JsonPropertyName("dependencies")] public Dictionary<string, string> Dependencies { get; set; }

        [JsonPropertyName("assets")] public List<Asset> Assets { get; set; }

        [JsonPropertyName("remove_files")] public List<string> RemoveFiles { get; set; }

        [JsonPropertyName("scripts")] public Scripts Scripts { get; set; }
    }

    public class Asset
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("urls")] public List<string> Urls { get; set; }

        [JsonPropertyName("placements")] public List<Placement> Placements { get; set; }
    }

    public class Placement
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("src")] public string Src { get; set; }

        [JsonPropertyName("dest")] public string Dest { get; set; }
    }

    public class Scripts
    {
        [JsonPropertyName("post_install")] public List<string> PostInstall { get; set; }

        [JsonPropertyName("post_uninstall")] public List<string> PostUninstall { get; set; }
    }
}