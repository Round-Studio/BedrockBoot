using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Chunker.Base;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;
using BedrockBoot.Core.Models.Download;
using Octokit;
using Round.SDK.Entity;

namespace BedrockBoot.Chunker;

public class Chunker
{
    public static string ChunkerDownloadManifest { get; } = "https://download.roundstudio.top/files/chunker-cli/manifest.json";
    public static string ChunkerDownloadRoot { get; } = "https://download.roundstudio.top";

    public static readonly string ChunkerFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2", "BedrockBoot.Chunker");

    public static readonly string ChunkerDownloadFolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2", "BedrockBoot.Chunker","Download");

    public static readonly string ChunkerPath =
        Path.Combine(ChunkerFolderPath, "chunker-cli.jar");

    public static List<string> SupportJava { get; } = new()
    {
        "1.8.8",
        "1.9.0",
        "1.10.0",
        "1.11.0",
        "1.12.0",
        "1.13.0",
        "1.14.0",
        "1.15.0",
        "1.16.0",
        "1.17.0",
        "1.18.0",
        "1.19.0",
        "1.20.0",
        "1.21.0"
    };

    public static List<string> SupportBedrock { get; } = new()
    {
        "1.12.0",
        "1.13.0",
        "1.14.0",
        "1.16.0",
        "1.17.0",
        "1.18.0",
        "1.19.0",
        "1.20.0",
        "1.21.0",
        "1.26.0"
    };

    public static bool CheckChunker() => File.Exists(ChunkerPath);
    
    public static async Task DownloadChunker(DownloadType dowType,IProgress<DownloadProgressEventArgs> progress)
    {
        if (dowType == DownloadType.DownloadSource)
        {
            HttpClient _httpClient;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BedrockBoot.Chunker");
            string jsonString = await _httpClient.GetStringAsync(ChunkerDownloadManifest);

            var downManifest = ConfigEntity<ChunkerManifest>.JsonDeserialize(jsonString);
            var parts = downManifest.Parts.Select(uri => ($"{ChunkerDownloadRoot}{uri}",
                Path.Combine(ChunkerDownloadFolderPath, new Uri($"{ChunkerDownloadRoot}{uri}").Segments.Last()))).ToList();

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

            var down = new GithubFilesDownload();
            await down.DownloadAsync(url, ChunkerPath, new Progress<DownloadProgress>(p =>
            {
                progress.Report(new DownloadProgressEventArgs("下载 Chunker", (int)p.ProgressPercentage));
            }));
        }
    }
}