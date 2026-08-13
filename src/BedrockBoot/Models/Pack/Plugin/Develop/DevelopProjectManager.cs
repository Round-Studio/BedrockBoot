using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using BedrockBoot.Base.Entry.Info.Develop;
using BedrockBoot.Models.Global;
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
        Projects = projects.Select(x => { return GetProjectInfo(x); }).ToList();
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