using BedrockBoot.LeviLamina.Base.Enum;

namespace BedrockBoot.LeviLamina.Base.Entry.Porgress;

public class InstallerProgress
{
    public string Message { get; set; }
    public double Progress { get; set; }
    public InstallerStatus Status { get; set; }
}