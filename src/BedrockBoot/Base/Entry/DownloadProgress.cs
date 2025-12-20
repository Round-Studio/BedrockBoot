namespace BedrockBoot.Base.Entry;

public class DownloadProgress
{
    /// <summary>
    /// 获取总字节数。如果未知，则为 -1。
    /// </summary>
    public long TotalBytes { get; init; } = -1;

    /// <summary>
    /// 获取已下载的字节数。
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// 获取下载进度的百分比 (0-100)。如果总大小未知，则为 -1。
    /// </summary>
    public double ProgressPercentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : -1;
}