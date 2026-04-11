using System.Diagnostics;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Chunker.Base.Entry;
using BedrockBoot.Chunker.Base.Entry.Info;
using BedrockBoot.Chunker.Base.Manifest;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using BedrockBoot.Core.Models.Download;
using Octokit;
using Round.SDK.Entity;

namespace BedrockBoot.Chunker;

public class Chunker
{
    #region 静态方法

    public static string ChunkerDownloadManifest { get; } =
        "https://download.roundstudio.top/files/chunker-cli/manifest.json";

    public static string ChunkerDownloadRoot { get; } = "https://download.roundstudio.top";

    public static readonly string ChunkerFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2", "BedrockBoot.Chunker");

    public static readonly string ChunkerDownloadFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2", "BedrockBoot.Chunker", "Download");

    public static readonly string ChunkerPath =
        Path.Combine(ChunkerFolderPath, "chunker-cli.jar");

    public static JavaInfo? DefaultJvmInfo { get; set; } = null;

    public static List<string> SupportJava { get; } = new()
    {
        "1.21.11",
        "1.21.10",
        "1.21.9",
        "1.21.8",
        "1.21.7",
        "1.21.6",
        "1.21.5",
        "1.21.4",
        "1.21.3",
        "1.21.2",
        "1.21.1",
        "1.21.0",
        "1.20.6",
        "1.20.5",
        "1.20.4",
        "1.20.3",
        "1.20.2",
        "1.20.1",
        "1.20.0",
        "1.19.4",
        "1.19.3",
        "1.19.2",
        "1.19.1",
        "1.19.0",
        "1.18.2",
        "1.18.1",
        "1.18.0",
        "1.17.1",
        "1.17.0",
        "1.16.5",
        "1.16.4",
        "1.16.3",
        "1.16.2",
        "1.16.1",
        "1.16.0",
        "1.15.2",
        "1.15.1",
        "1.15.0",
        "1.14.4",
        "1.14.3",
        "1.14.2",
        "1.14.1",
        "1.14.0",
        "1.13.2",
        "1.13.1",
        "1.13.0",
        "1.12.2",
        "1.12.1",
        "1.12.0",
        "1.11.2",
        "1.11.1",
        "1.11.0",
        "1.10.2",
        "1.10.1",
        "1.10.0",
        "1.9.3",
        "1.9.2",
        "1.9.1",
        "1.9.0",
        "1.8.8"
    };

    public static List<string> SupportBedrock { get; } = new()
    {
        "1.26.0",
        "1.21.0",
        "1.20.0",
        "1.19.0",
        "1.18.0",
        "1.17.0",
        "1.16.0",
        "1.14.0",
        "1.13.0",
        "1.12.0"
    };

    public static bool CheckChunker() => File.Exists(ChunkerPath);

    public static bool CheckJvm(JavaInfo jvmInfo)
    {
        if (jvmInfo.MajorVersion >= 17)
            return true;

        return false;
    }

    public static async Task DownloadChunker(DownloadType dowType, IProgress<DownloadProgressEventArgs> progress)
    {
        if (dowType == DownloadType.DownloadSource)
        {
            HttpClient _httpClient;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BedrockBoot.Chunker");
            string jsonString = await _httpClient.GetStringAsync(ChunkerDownloadManifest);

            var downManifest = ConfigEntity<ChunkerManifest>.JsonDeserialize(jsonString);
            var parts = downManifest.Parts.Select(uri => ($"{ChunkerDownloadRoot}{uri}",
                    Path.Combine(ChunkerDownloadFolderPath, new Uri($"{ChunkerDownloadRoot}{uri}").Segments.Last())))
                .ToList();

            var tasks = parts.Select(p => new SingleThreadDownloader().DownloadAsync(p.Item1, p.Item2,
                new Progress<DownloadProgress>(pro =>
                {
                    progress.Report(new($"下载 {p.Item1}", (int)pro.ProgressPercentage));
                })));

            Task.WaitAll(tasks.ToArray());

            var count = 0;
            try
            {
                using var outputStream = File.Create(ChunkerPath);
                foreach (var partFile in parts.Select(p => p.Item2))
                {
                    using var inputStream = File.OpenRead(partFile);
                    inputStream.CopyTo(outputStream);
                    progress.Report(new($"合并 {partFile}", count / parts.Count));
                    count++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"合并文件失败: {ex.Message}", ex);
            }
        }

        if (dowType == DownloadType.Github)
        {
            progress.Report(new DownloadProgressEventArgs("获取更新...", 10));
            var github = new GitHubClient(new ProductHeaderValue("BedrockBoot"));

            // 获取指定仓库的所有发布
            var owner = "HiveGamesOSS";
            var repo = "Chunker";
            var releases = await github.Repository.Release.GetLatest(owner, repo);
            var url = releases.Assets.ToList().Find(x => x.Name.Contains(".jar")).BrowserDownloadUrl;

            var down = new GithubFilesDownloader();
            await down.DownloadAsync(url, ChunkerPath,
                new Progress<DownloadProgress>(p =>
                {
                    progress.Report(new DownloadProgressEventArgs("下载 Chunker", (int)p.ProgressPercentage));
                }));
        }
    }

    #endregion

    public void BeginChunker(ChunkerInfo info)
    {
        if (!CheckChunker()) throw new Exception("未安装 Chunker");
        if (!CheckJvm(info.JvmInfo)) throw new Exception("Jvm 版本不符合最低版本要求 (JVM>=17)");

        Console.WriteLine($@"将使用 {info.JvmInfo.JavaPath}");

        ProcessStartInfo startInfo = new ProcessStartInfo(info.JvmInfo.JavaPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8, // 设置 UTF-8 编码以正确显示中文
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        if (info.ChunkerType == ChunkerType.BedrockToJava)
        {
            if (!SupportJava.Contains(info.JavaEditionVersion!))
                throw new Exception($"不支持的游戏版本 {info.JavaEditionVersion}");

            startInfo.ArgumentList.Add("-jar");
            startInfo.ArgumentList.Add(ChunkerPath);
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(info.BedrockWorldFolder);
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add($"JAVA_{info.JavaEditionVersion.Replace(".", "_")}");
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(info.JavaWorldFolder);

            Console.WriteLine($@"转换参数: 从 Bedrock 到 Java {info.JavaEditionVersion}");
        }
        else
        {
            if (!SupportBedrock.Contains(info.BedrockEditionVersion!))
                throw new Exception($"不支持的游戏版本 {info.BedrockEditionVersion}");

            startInfo.ArgumentList.Add("-jar");
            startInfo.ArgumentList.Add(ChunkerPath);
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(info.JavaWorldFolder);
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add($"BEDROCK_{info.BedrockEditionVersion!.Replace(".", "_")}");
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(info.BedrockWorldFolder);

            Console.WriteLine($@"转换参数: 从 Java 到 Bedrock {info.BedrockEditionVersion}");
        }

        using (var process = new Process())
        {
            process.StartInfo = startInfo;

            // 设置实时输出处理
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($@"[Chunker] {e.Data}");

                    var log = e.Data.Replace("%", "");
                    if (double.TryParse(log, out double result))
                    {
                        info.Progress?.Report(result);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($@"[Chunker 错误] {e.Data}");
                }
            };

            try
            {
                Console.WriteLine(@"开始转换，请等待...");
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Console.WriteLine($@"转换完成，退出代码: {process.ExitCode}");

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Chunker 转换失败，退出代码: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"执行过程中发生错误: {ex.Message}");
                throw;
            }
        }
    }
}