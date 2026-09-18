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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Game.Pack.Screenshots;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Pack.Game.Screenshots;

public class ScreenshotsManager
{
    public ScreenshotsManager(VersionConfig versionInfo)
    {
        VersionConfig = versionInfo;
    }

    public VersionConfig VersionConfig { get; set; }

    public Dictionary<string, List<ScreenshotsInfo>> GetScreenshots()
    {
        var result = new Dictionary<string, List<ScreenshotsInfo>>();
        var users = GetInstanceScreenshotsPath();

        foreach (var user in users)
        {
            if (!Directory.Exists(user.Value)) continue;

            var files = Directory.GetFiles(user.Value, "*.jpeg", SearchOption.AllDirectories)
                .ToList();
            var resultInfos = new List<ScreenshotsInfo>();

            files.ForEach(file =>
            {
                var confFile = file.Replace(".jpeg", ".json");
                var conf = new ConfigEntity<ScreenshotsInfo>(confFile, false);
                conf.Data.FilePath = file;
                resultInfos.Add(conf.Data);
            });

            result.Add(user.Key, resultInfos);
        }

        return result;
    }

    public Dictionary<string, string> GetInstanceScreenshotsPath()
    {
        var result = new Dictionary<string, string>();
        if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.UWP)
            result.Add("Shared",
                IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ScreenshotFolder));
        else if (VersionConfig.Info.BuildType == MinecraftBuildTypeVersion.GDK)
            IsolationCore.GetInstanceUsers(VersionConfig).ForEach(user =>
            {
                result.Add(user,
                    IsolationCore.GetInstanceFolderPath(VersionConfig, InstanceFolderType.ScreenshotFolder, user));
            });

        return result;
    }
}