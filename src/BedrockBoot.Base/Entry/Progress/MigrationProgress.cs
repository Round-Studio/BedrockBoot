namespace BedrockBoot.Base.Entry.Progress;

public class MigrationProgress
{
    public int FileCountTotal { get; set; } = 0;
    public int CurrentFile { get; set; }
    public string Status { get; set; }
    public bool IsCompleted { get; set; }
    public double Percentage { get; set; }
    public string CurrentType { get; set; }
}