using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Entity.Game.Mods.Arke;

namespace BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;

public class ArkeShaderPacksManager
{
    private readonly VersionConfig _instance;

    public ArkeShaderPacksManager(VersionConfig instance)
    {
        _instance = instance;
    }
    
    public string GetShaderPacksDir()
    {
        return Path.Combine(_instance.VersionPath, "config", "BedrockBoot2", "levilamina", "ll.mods",
            "arke", "shaderpacks");
    }
    
    public List<ArkeShaderPackInfo> GetShaderPacks()
    {
        var shaderPacks = new List<ArkeShaderPackInfo>();
        var shaderPacksDir = GetShaderPacksDir();
        if (!Directory.Exists(shaderPacksDir))
        {
            Directory.CreateDirectory(shaderPacksDir);
            return shaderPacks;
        }

        Directory.GetFiles(shaderPacksDir, "*.zip").ToList().ForEach(file =>
        {
            shaderPacks.Add(new()
            {
                PackName = Path.GetFileName(file),
                PackPath = file,
                PackSize = (new FileInfo(file)).Length
            });
        });

        return shaderPacks;
    }
}