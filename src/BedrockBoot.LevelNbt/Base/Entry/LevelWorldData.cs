using BedrockBoot.LevelNbt.Base.Enum;

namespace BedrockBoot.LevelNbt.Base.Entry;

/// <summary>
///     存储提取的世界数据（增强版）
/// </summary>
public class LevelWorldData
{
    public string LevelName { get; set; } = "";
    public long RandomSeed { get; set; }
    public int GameType { get; set; }
    public bool CheatsEnabled { get; set; }
    public bool CommandsEnabled { get; set; }
    public bool IsHardCore { get; set; }
    public string FlatWorldLayers { get; set; } = "";
    public int SpawnX { get; set; }
    public int SpawnY { get; set; }
    public int SpawnZ { get; set; }
    public long Time { get; set; }
    public long LastPlayed { get; set; }

    public override string ToString()
    {
        return $"存档名称: {LevelName}\n" +
               $"随机种子: {RandomSeed}\n" +
               $"游戏模式: {(GameModes)GameType}\n" +
               $"作弊开启: {CheatsEnabled}\n" +
               $"管理员权限: {CommandsEnabled}\n" +
               $"极限模式: {IsHardCore}\n" +
               $"出生点: ({SpawnX}, {SpawnY}, {SpawnZ})\n" +
               $"游戏时间: {Time}\n" +
               $"最后一次游玩: {LastPlayed}\n" +
               $"超平坦预设: {(string.IsNullOrEmpty(FlatWorldLayers) ? "否" : "是")}";
    }
}