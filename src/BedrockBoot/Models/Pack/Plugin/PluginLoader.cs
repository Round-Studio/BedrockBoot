using System;
using System.Collections.Generic;
using System.IO;
using BedrockBoot.Models.Global;
using Round.SDK.Entry;
using Round.SDK.Plugin;
using Round.SDK.Plugin.BedrockBoot;

namespace BedrockBoot.Models.Pack.Plugin;

public class PluginLoader
{
    public static List<PackConfig> Plugins { get; set; } = new List<PackConfig>();

    public static async System.Threading.Tasks.Task LoadAll()
    {
        Console.WriteLine(@"开始加载插件。");
        if (!Directory.Exists(PathsList.PluginPath)) Directory.CreateDirectory(PathsList.PluginPath);
        Console.WriteLine($@"插件文件夹：{PathsList.PluginPath}");

        var files = Directory.GetFiles(PathsList.PluginPath, "*.rplck");
        foreach (var file in files)
        {
            try
            {
                var loader = new PlugLoader(typeof(IPluginBedrockBoot))
                {
                    ExtractPath = Path.Combine(PathsList.TempPath)
                };
                var plugin = loader.Load(file);

                var config = loader.GetPackConfig();
                Plugins.Add(config);

                // 执行方法
                loader.InitializePlugin();
            }
            catch
            {
            }
        }
    }
}