namespace BedrockBoot.Chunker.Event;

public class DownloadProgressEventArgs : EventArgs
{
    public string Status { get; }
    public int Percentage { get; }

    public DownloadProgressEventArgs(string status, int percentage)
    {
        Status = status;
        Percentage = percentage;
    }
}