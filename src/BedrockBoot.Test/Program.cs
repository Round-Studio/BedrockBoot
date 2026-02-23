using BedrockBoot.Chunker;
using BedrockBoot.Chunker.Base.Enum;
using BedrockBoot.Chunker.Event;

public class Program
{
    private static async Task Main()
    {
        await Chunker.DownloadChunker(DownloadType.Github,
            new Progress<DownloadProgressEventArgs>(p => Console.WriteLine($"{p.Status} {p.Percentage}")));
    }
}