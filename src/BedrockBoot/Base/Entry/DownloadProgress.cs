using System;

namespace BedrockBoot.Base.Entry;

public class DownloadProgress
{
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double ProgressPercentage => TotalBytes > 0 
        ? Math.Min(100, Math.Round((double)DownloadedBytes / TotalBytes * 100, 2))
        : 0;
    public double BytesPerSecond { get; set; }
    public double EstimatedRemainingSeconds { get; set; }
}