using System.Collections.Generic;
using System.IO;
using System.IO.Compression; // 添加这个命名空间
using System.Linq;
using System.Text;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.LevelNbt;
using BedrockBoot.LevelNbt.Base.Entry;

namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveSerializer
{
    private LevelDatParser _datParser;

    public ArchiveSerializer(string levelPath)
    {
        _datParser = new LevelDatParser(Path.Combine(levelPath, "level.dat"));
    }

    public LevelWorldData Parser() => _datParser.WorldData;
}