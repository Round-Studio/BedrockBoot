using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Isolation;

namespace BedrockBoot.Models.Pack.Game.Isolation;

public class IsolationMigration
{
    public VersionConfig NewConfig { get; set; }
    public VersionConfig OldConfig { get; set; }

    public IsolationMigration(VersionConfig newConfig,VersionConfig oldConfig)
    {
        NewConfig = newConfig;
        OldConfig = oldConfig;
    }

    public void Migration(MigrationConfig migrationConfig)
    {
        
    }
}