namespace BedrockBoot.Base.Entry.Game.Pack.Isolation;

public class MigrationConfig
{
    public bool IsEnableResourcePack { get; set; } = true;
    public bool IsEnableBehaviorPack { get; set; } = true;
    public bool IsEnableArchive { get; set; } = true;
    public bool IsEnableConfig { get; set; } = true;
    public VersionConfig NewVersionConfig { get; set; }
    public VersionConfig OldVersionConfig { get; set; }
}