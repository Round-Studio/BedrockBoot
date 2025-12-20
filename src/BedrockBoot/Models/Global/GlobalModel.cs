using System.Windows.Forms.VisualStyles;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Entity;
using BedrockBoot.Models.Task;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Windows;
using BedrockLauncher.Core;
using Downloader;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class GlobalModel
{
    public static ConfigEntity<ConfigEntry> Config;
    public static MainWindow MainWindow;
    public static BedrockCore BedrockCore { get; set; }
    public static TaskManager TaskManager { get; set; } = new();
    public static bool IsAbleToLaunchGame { get; set; } = false;
    public static FunctionOptionEntry FunctionOption { get; set; }
    public static ProtocolService ProtocolService { get; set; } = new  ProtocolService();
    public static DownloadConfiguration DownloadConfiguration => new DownloadConfiguration()
    {
        BufferBlockSize = 10240,
        ChunkCount = Config.Data.DownloadChunkCount,
        MaximumMemoryBufferBytes = 1024 * 1024 * 50,
        ParallelDownload = true,
        ParallelCount = 4,
        Timeout = 1000,
        RangeDownload = false,
        RangeLow = 0,
        RangeHigh = 0,
        ClearPackageOnCompletionWithFailure = true,
        MinimumSizeOfChunking = 1024,
        ReserveStorageSpaceBeforeStartingDownload = true
    };
}