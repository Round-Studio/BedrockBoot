namespace BedrockBoot.Base.Entry.Info.Download;

public class ResourceFileInfo
{
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public uint FileSize { get; set; } = 0;
    public string? Version { get; set; }
    public Action<string>? OnDownload { get; set; }
    public Action<string>? OnSaveAs { get; set; }
    public bool IsEnableSaveAs { get; set; } = false;
}