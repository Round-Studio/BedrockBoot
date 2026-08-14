using BedrockLauncher.Core;

namespace BedrockBoot.Downloader;

public class DownloaderCore
{
    public static BedrockCore BedrockCore { get; set; }
    public static void InitCore()
    {
        BedrockCore = new();
        _ = BedrockCore.InitAsync();
    }
}