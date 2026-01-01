using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Mods;

public class ModsManager
{
    public VersionConfig VersionInfo { get; set; }
    public List<ModInfo> Mods => ModsConfig.Data;
    public ConfigEntity<List<ModInfo>> ModsConfig { get; private set; }
    public Action? RefreshCallBack { get; set; }

    public ModsManager(VersionConfig versionInfo)
    {
        VersionInfo = versionInfo;
        ModsConfig =
            new ConfigEntity<List<ModInfo>>(
                Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods.json"));
        ModsConfig.Load();
    }

    public List<ModInfo> RefreshMods(bool isRefresh = false)
    {
        ModsConfig.Load();
        var path = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var files = Directory.GetFiles(path, "*.dll").ToList();
        var modFiles = new HashSet<string>(Mods.Select(m => m.File)).ToList();
        files.ForEach(file =>
        {
            if (!modFiles.Contains(file))
            {
                AddMod(new ModInfo()
                {
                    File = file
                });
            }
        });
        modFiles.ForEach(file =>
        {
            if (!files.Contains(file))
            {
                ModsConfig.Data.Remove(ModsConfig.Data.Find(m => m.File == file));
            }
        });
        ModsConfig.Save();

        if (isRefresh) RefreshCallBack?.Invoke();
        return Mods;
    }

    public void AddMod(ModInfo mod)
    {
        ModsConfig.Data.Add(mod);
        ModsConfig.Save();
    }

    public void InjectAll(int processId)
    {
        Mods.ForEach(x => System.Threading.Tasks.Task.Run(() => x.Inject(processId)));
    }
}