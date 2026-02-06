using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Entry.Progress;

public class IntegrationProgress
{
    public double Progress { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class InstallIntegrationProgress
{
    public double Progress { get; set; }
    public string Message { get; set; } = string.Empty;
    public InstallIntegrationProgressType Status { get; set; }
}