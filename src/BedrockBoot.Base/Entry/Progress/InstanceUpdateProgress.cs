using BedrockBoot.Base.Enum.Type.Progress.Steps;

namespace BedrockBoot.Base.Entry.Progress;

public class InstanceUpdateProgress
{
    public InstanceUpdateStep Step { get; set; }
    public double Progress { get; set; }
    public string Message { get; set; }
    public string Detailed { get; set; }
}