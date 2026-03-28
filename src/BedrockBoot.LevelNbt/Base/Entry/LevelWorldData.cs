namespace BedrockBoot.LevelNbt.Base.Entry;

public class LevelWorldData
{
    public int HeaderVersion { get; set; }

    // --- 核心 ---
    public string LevelName { get; set; } = "";
    public long RandomSeed { get; set; }
    public int GameType { get; set; }
    public int Difficulty { get; set; }
    public long Time { get; set; }
    public long LastPlayed { get; set; }
    public int Generator { get; set; }
    public int StorageVersion { get; set; }
    public string InventoryVersion { get; set; } = "";
    
    // --- 规则开关 ---
    public bool CheatsEnabled { get; set; }
    public bool IsHardCore { get; set; }
    public bool CommandsEnabled { get; set; }
    public bool CommandBlocksEnabled { get; set; }
    public bool CommandBlockOutput { get; set; }
    public bool SendCommandFeedback { get; set; }
    public bool HasBonusChest { get; set; }
    public bool HasStartMap { get; set; }
    public bool DoImmediateRespawn { get; set; }
    public bool RecipeUnlock { get; set; }
    public bool LimitedCrafting { get; set; }
    public bool TexturepacksRequired { get; set; }

    // --- 界面 ---
    public bool ShowCoordinates { get; set; }
    public bool ShowDaysPlayed { get; set; }
    public bool ShowDeathMessages { get; set; }
    public bool ShowRecipeMessages { get; set; }
    public bool ShowTags { get; set; }
    public bool ShowBorderEffect { get; set; }

    // --- 生物与环境 ---
    public bool DoDaylightCycle { get; set; }
    public bool DoWeatherCycle { get; set; }
    public bool DoMobSpawning { get; set; }
    public bool DoInsomnia { get; set; }
    public bool MobGriefing { get; set; }
    public bool DoMobLoot { get; set; }
    public bool DoEntityDrops { get; set; }
    public bool DoTileDrops { get; set; }
    public bool DoFireTick { get; set; }
    public bool TntExplodes { get; set; }
    public bool RespawnBlocksExplode { get; set; }

    // --- 玩家规则 ---
    public bool KeepInventory { get; set; }
    public bool NaturalRegeneration { get; set; }
    public bool Pvp { get; set; }
    public bool FallDamage { get; set; }
    public bool FireDamage { get; set; }
    public bool DrowningDamage { get; set; }
    public bool FreezeDamage { get; set; }

    // --- 坐标 ---
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
    public int SpawnZ { get; set; }

    // --- 权限/能力 ---
    public bool Mine { get; set; }
    public bool Build { get; set; }
    public bool AttackMobs { get; set; }
    public bool AttackPlayers { get; set; }
    public bool DoorsAndSwitches { get; set; }
    public bool OpenContainers { get; set; }
    public bool Op { get; set; }
    public bool Teleport { get; set; }
    public bool Flying { get; set; }
    public bool Mayfly { get; set; }
    public bool Instabuild { get; set; }
    public bool Invulnerable { get; set; }
    public bool NoClip { get; set; }
    public bool WorldBuilder { get; set; } // 映射到 NBT 的 lightning

    // --- 速度 ---
    public float WalkSpeed { get; set; }
    public float FlySpeed { get; set; }
    public float VerticalFlySpeed { get; set; }
}