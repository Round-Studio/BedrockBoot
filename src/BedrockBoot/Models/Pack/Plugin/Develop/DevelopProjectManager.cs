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