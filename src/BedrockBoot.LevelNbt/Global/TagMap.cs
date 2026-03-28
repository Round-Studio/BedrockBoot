using BedrockBoot.LevelNbt.Base.Entry;

namespace BedrockBoot.LevelNbt.Global;

public static class TagMap
{
    public static readonly Dictionary<string, string> TagsMap = new()
    {
        // --- 核心 & 基础 (根目录下) ---
        { "LevelName", nameof(LevelWorldData.LevelName) },
        { "RandomSeed", nameof(LevelWorldData.RandomSeed) },
        { "GameType", nameof(LevelWorldData.GameType) },
        { "Difficulty", nameof(LevelWorldData.Difficulty) },
        { "Time", nameof(LevelWorldData.Time) },
        { "LastPlayed", nameof(LevelWorldData.LastPlayed) },
        { "Generator", nameof(LevelWorldData.Generator) },
        { "StorageVersion", nameof(LevelWorldData.StorageVersion) },
        { "IsHardcore", nameof(LevelWorldData.IsHardCore) },
        { "InventoryVersion", nameof(LevelWorldData.InventoryVersion) },
        
        // --- 坐标 ---
        { "SpawnX", nameof(LevelWorldData.SpawnX) },
        { "SpawnY", nameof(LevelWorldData.SpawnY) },
        { "SpawnZ", nameof(LevelWorldData.SpawnZ) },

        // --- 规则 (根目录下的小写键名) ---
        { "cheatsEnabled", nameof(LevelWorldData.CheatsEnabled) },
        { "commandsEnabled", nameof(LevelWorldData.CommandsEnabled) },
        { "commandblocksenabled", nameof(LevelWorldData.CommandBlocksEnabled) },
        { "commandblockoutput", nameof(LevelWorldData.CommandBlockOutput) },
        { "sendcommandfeedback", nameof(LevelWorldData.SendCommandFeedback) },
        { "bonusChestEnabled", nameof(LevelWorldData.HasBonusChest) },
        { "startWithMapEnabled", nameof(LevelWorldData.HasStartMap) },
        { "doimmediaterespawn", nameof(LevelWorldData.DoImmediateRespawn) },
        { "recipesunlock", nameof(LevelWorldData.RecipeUnlock) },
        { "dolimitedcrafting", nameof(LevelWorldData.LimitedCrafting) },
        { "texturePacksRequired", nameof(LevelWorldData.TexturepacksRequired) },

        // --- 界面 ---
        { "showcoordinates", nameof(LevelWorldData.ShowCoordinates) },
        { "showdaysplayed", nameof(LevelWorldData.ShowDaysPlayed) },
        { "showdeathmessages", nameof(LevelWorldData.ShowDeathMessages) },
        { "showrecipemessages", nameof(LevelWorldData.ShowRecipeMessages) },
        { "showtags", nameof(LevelWorldData.ShowTags) },
        { "showbordereffect", nameof(LevelWorldData.ShowBorderEffect) },

        // --- 生物与环境 ---
        { "dodaylightcycle", nameof(LevelWorldData.DoDaylightCycle) },
        { "doweathercycle", nameof(LevelWorldData.DoWeatherCycle) },
        { "domobspawning", nameof(LevelWorldData.DoMobSpawning) },
        { "doinsomnia", nameof(LevelWorldData.DoInsomnia) },
        { "mobgriefing", nameof(LevelWorldData.MobGriefing) },
        { "domobloot", nameof(LevelWorldData.DoMobLoot) },
        { "doentitydrops", nameof(LevelWorldData.DoEntityDrops) },
        { "dotiledrops", nameof(LevelWorldData.DoTileDrops) },
        { "dofiretick", nameof(LevelWorldData.DoFireTick) },
        { "tntexplodes", nameof(LevelWorldData.TntExplodes) },
        { "respawnblocksexplode", nameof(LevelWorldData.RespawnBlocksExplode) },

        // --- 玩家规则 ---
        { "keepinventory", nameof(LevelWorldData.KeepInventory) },
        { "naturalregeneration", nameof(LevelWorldData.NaturalRegeneration) },
        { "pvp", nameof(LevelWorldData.Pvp) },
        { "falldamage", nameof(LevelWorldData.FallDamage) },
        { "firedamage", nameof(LevelWorldData.FireDamage) },
        { "drowningdamage", nameof(LevelWorldData.DrowningDamage) },
        { "freezedamage", nameof(LevelWorldData.FreezeDamage) },

        // --- Abilities 嵌套内的键名 (由递归方法自动匹配) ---
        { "mine", nameof(LevelWorldData.Mine) },
        { "build", nameof(LevelWorldData.Build) },
        { "attackmobs", nameof(LevelWorldData.AttackMobs) },
        { "attackplayers", nameof(LevelWorldData.AttackPlayers) },
        { "doorsandswitches", nameof(LevelWorldData.DoorsAndSwitches) },
        { "opencontainers", nameof(LevelWorldData.OpenContainers) },
        { "op", nameof(LevelWorldData.Op) },
        { "teleport", nameof(LevelWorldData.Teleport) },
        { "flying", nameof(LevelWorldData.Flying) },
        { "mayfly", nameof(LevelWorldData.Mayfly) },
        { "instabuild", nameof(LevelWorldData.Instabuild) },
        { "invulnerable", nameof(LevelWorldData.Invulnerable) },
        { "lightning", nameof(LevelWorldData.WorldBuilder) }, // JSON 中叫 lightning
        { "noclip", nameof(LevelWorldData.NoClip) },
        { "walkSpeed", nameof(LevelWorldData.WalkSpeed) },
        { "flySpeed", nameof(LevelWorldData.FlySpeed) },
        { "verticalFlySpeed", nameof(LevelWorldData.VerticalFlySpeed) }
    };
}