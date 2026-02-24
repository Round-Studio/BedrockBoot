using BedrockBoot.Chunker.Base.Enum;

namespace BedrockBoot.Chunker.Base.Entry;

public class ChunkerInfo
{
    public ChunkerType ChunkerType { get; set; }
    public string JavaWorldFolder { get; set; } = string.Empty;
    public string BedrockWorldFolder { get; set; } = string.Empty;
    public IProgress<double>? Progress { get; set; }
}