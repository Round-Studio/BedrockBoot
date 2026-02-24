using BedrockBoot.Chunker.Base.Entry.Info;
using BedrockBoot.Chunker.Base.Enum;

namespace BedrockBoot.Chunker.Base.Entry;

public class ChunkerInfo
{
    public required JavaInfo JvmInfo { get; set; }
    public ChunkerType ChunkerType { get; set; }
    public string JavaWorldFolder { get; set; } = string.Empty;
    public string BedrockWorldFolder { get; set; } = string.Empty;
    public string? JavaEditionVersion { get; set; }
    public string? BedrockEditionVersion { get; set; }
    public IProgress<double>? Progress { get; set; }
}