using BedrockBoot.Base.Enum.Type.Export;

namespace BedrockBoot.Base.Entry.Game.Pack.Archive.Export;

public class ExportConfig
{
    public ArchiveExportType ExportType { get; set; } = ArchiveExportType.World;
    public ArchiveInfo? ArchiveInfo { get; set; } = null;
    
    public bool AllowRandomSeed { get; set; } = false;
    public bool LockTemplateOptions { get; set; } = true;
    public bool PortableBedrockBootConfig { get; set; } = true;
    public string PackVersion { get; set; } = "1.0.0";
    public string PackName { get; set; } = string.Empty;
    public string PackDescription { get; set; } = string.Empty;
}