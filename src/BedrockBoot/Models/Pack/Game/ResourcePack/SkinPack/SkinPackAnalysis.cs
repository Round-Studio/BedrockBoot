using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BedrockBoot.Models.Pack.Game.ResourcePack.SkinPack;

public class SkinPackAnalysis
{
    private readonly string _packFolder;

    public SkinPackAnalysis(string packFolder)
    {
        _packFolder = packFolder;
    }

    public List<string> GetAllSkin()
    {
        return Directory.GetFiles(_packFolder, "*.png", SearchOption.AllDirectories).ToList();
    }
}