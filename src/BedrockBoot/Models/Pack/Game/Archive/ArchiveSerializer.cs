using System.Collections.Generic;
using System.IO;
using System.IO.Compression; // 添加这个命名空间
using System.Linq;
using System.Text;
using System.Text.Json;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
namespace BedrockBoot.Models.Pack.Game.Archive;

public class ArchiveSerializer : ArchiveInfo
{
    public ArchiveSerializer(string levelPath)
    {
        Path = levelPath;
    }
}