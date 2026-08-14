using System.Diagnostics;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Downloader.Files;
using BedrockBoot.Models.Global;
using Octokit;

namespace BedrockBoot.Downloader.Proton;

public class ProtonDownloader
{
    public ProtonDownloader() { }
    public const string ProtonVersion = "GDK-Proton10-32";

    public static bool IsInstalledProtonVersion(string version = ProtonVersion)
    {
        string protonWorkPath = Path.Combine(PathsList.ProtonPath, "work");
        string protonBinPath = Path.Combine(protonWorkPath, version);

        return Directory.Exists(protonBinPath);
    }

    public async Task Download(IProgress<DownloadProgress> progress, CancellationToken ct = default)
    {
#if WINDOWS
        Console.WriteLine(@"Windows 下无需下载 ProtonGDK");
        return;
#endif
        
        // 1. 修正异步获取逻辑，避免使用 .Result
        var client = new GitHubClient(new ProductHeaderValue("BedrockBoot.Linux"));
        // var releases = await client.Repository.Release.GetAll("Weather-OS", "GDK-Proton");
        var releases = await client.Repository.Release.GetAll("LukasPAH", "GDK-Proton-Custom");
        var latest = releases.FirstOrDefault(x => x.Name == ProtonVersion);

        if (latest == null)
            throw new Exception($"未找到版本: {ProtonVersion}");

        if (latest.Assets == null || latest.Assets.Count == 0)
            throw new Exception($"版本 {ProtonVersion} 的 Release 中没有可下载的资源文件");

        var downloadDir = Path.Combine(PathsList.ProtonPath, "download");
        var workDir = Path.Combine(PathsList.ProtonPath, "work");
        var fileName = Path.Combine(downloadDir, $"{ProtonVersion}.tar.gz");

        // 2. 确保必要的目录都已存在
        Directory.CreateDirectory(downloadDir);
        Directory.CreateDirectory(workDir);

        // 3. 执行下载
        var downloader = new GithubFilesDownloader();
        await downloader.DownloadAsync(latest.Assets[0].BrowserDownloadUrl, fileName, progress, ct);

        // 4. 使用更加健壮的 Process 启动方式 (解压)
        var startInfo = new ProcessStartInfo
        {
            FileName = "tar",
            RedirectStandardError = true, // 重定向错误流以便排查
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // 使用 ArgumentList 自动处理路径中的空格和引号，比直接拼 Arguments 字符串更安全
        startInfo.ArgumentList.Add("-xzf");
        startInfo.ArgumentList.Add(fileName);
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(workDir);

        using var process = Process.Start(startInfo);
        
        if (process == null)
            throw new Exception("无法启动 tar 进程。");

        // 先启动错误流读取，避免 stderr 缓冲区写满导致与 WaitForExitAsync 互相死锁
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        // 等待解压完成
        await process.WaitForExitAsync(ct);

        // 5. 检查解压结果
        if (process.ExitCode != 0)
        {
            // 如果报错，读取错误信息
            string errorOutput = await stderrTask;
            throw new Exception($"解压失败 (ExitCode {process.ExitCode}): {errorOutput}");
        }
    }
}