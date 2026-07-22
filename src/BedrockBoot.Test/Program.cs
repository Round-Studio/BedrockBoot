using BedrockBoot.Lip;
using BedrockBoot.Base.Entry.Progress;

var lipCore = new LipCore("github.com/LiteLDev/LeviLamina#client@26.20.4");

var progress = new Progress<DownloadProgress>(p =>
{
    Console.Write($"\r总大小: {p.TotalBytes / 1024 / 1024:F2} MB | " +
                  $"已下载: {p.DownloadedBytes / 1024 / 1024:F2} MB | " +
                  $"进度: {p.ProgressPercentage:F2}% | " +
                  $"速度: {p.BytesPerSecond / 1024 / 1024:F2} MB/s | " +
                  $"剩余: {p.EstimatedRemainingSeconds:F0}s | " +
                  $"{p.Message}");
});

await lipCore.Install("D:\\BedrockBoot\\bedrock_versions\\1.26.2004", progress);
Console.WriteLine("\n安装完成！");