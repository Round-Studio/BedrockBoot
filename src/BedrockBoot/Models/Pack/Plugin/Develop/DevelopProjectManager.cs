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
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Entity.Info.Develop;
using Round.SDK.Entity;
using Round.SDK.Entry;

namespace BedrockBoot.Models.Pack.Plugin.Develop;

public class DevelopProjectManager
{
    public static List<ProjectInfo> Projects { get; private set; } = new();

    public static void Init()
    {
        Projects.Clear();
        var file = Path.Combine(PathsList.ConfigFolderPath, "projects.json");
        var conf = new ConfigEntity<List<string>>(file);

        if (conf.Data == null)
            conf.Data = new();

        conf.Save();

        var projects = conf.Data;
        // 跳过已被删除/移动的项目目录，避免列表里出现无效条目
        Projects = projects
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            .DistinctBy(path => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparer.OrdinalIgnoreCase)
            .Select(GetProjectInfo)
            .ToList();
    }

    public static void AddProject(string path, PackConfig config)
    {
        var file = Path.Combine(PathsList.ConfigFolderPath, "projects.json");
        var conf = new ConfigEntity<List<string>>(file);
        if (conf.Data == null)
            conf.Data = new();

        conf.Data.Add(path);
        conf.Save();

        var confProject = new ConfigEntity<PackConfig>(Path.Combine(path, "plugin.json"));
        confProject.Data = config;
        confProject.Save();
        Init();
    }

    public static ProjectInfo GetProjectInfo(string projectPath)
    {
        var conf = new ConfigEntity<PackConfig>(Path.Combine(projectPath, "plugin.json"), false);

        var result = new ProjectInfo()
        {
            ProjectPath = projectPath,
            ProjectName = conf.Data.PackName,
            PackInfo = conf.Data
        };

        return result;
    }
}