using BedrockBoot.Downloader.Enum;

namespace BedrockBoot.Downloader.Event.Progress;

public class DownloadGameProgress
{
    public DownloadGameProgress(GameInstallStatus status, string message, double progressPercentage)
    {
        Status = status;
        Message = message;
        ProgressPercentage = progressPercentage;
    }
    
    public GameInstallStatus Status { get; set; }
    public string Message { get; set; }
    public double ProgressPercentage { get; set; }
}