using System.IO;
using BedrockBoot.LevelNbt;
using BedrockBoot.LevelNbt.Base.Entry;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveSerializer
{
    private readonly LevelDatParser _datParser;

    public ArchiveSerializer(string levelPath)
    {
        _datParser = new LevelDatParser(Path.Combine(levelPath, "level.dat"));
    }

    public LevelWorldData Parser()
    {
        return _datParser.WorldData;
    }
}