/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Mods;
using Round.SDK.Entity;

namespace BedrockBoot.Core.Models.Pack.Game.Mods;

public class ModsManager
{
    public ModsManager(VersionConfig versionInfo)
    {
        VersionInfo = versionInfo;
        ModsConfig =
            new ConfigEntity<List<ModInfo>>(
                Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "mods.json"));
        ModsConfig.Load();
    }

    public VersionConfig VersionInfo { get; set; }
    public List<ModInfo> Mods => ModsConfig.Data;
    public ConfigEntity<List<ModInfo>> ModsConfig { get; }
    public Action? RefreshCallBack { get; set; }

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
                AddMod(new ModInfo
                {
                    File = file
                });
        });
        modFiles.ForEach(file =>
        {
            if (!files.Contains(file)) ModsConfig.Data.Remove(ModsConfig.Data.Find(m => m.File == file));
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

    /*public void InjectAll(int processId)
    {
        Mods.ForEach(x =>
        {
            if (!x.IsPreLoad) Task.Run(() => x.Inject(processId));
        });
    }*/
}