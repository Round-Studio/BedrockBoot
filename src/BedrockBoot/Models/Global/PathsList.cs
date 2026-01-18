using System;
using System.Collections.Generic;
using System.IO;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Config;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class PathsList
{
    public static readonly string RootConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio", "BedrockBoot2");

    public static readonly string ConfigPath = Path.Combine(RootConfigPath, "BedrockBoot.Config", "Config.json");
    public static readonly string LogPath = Path.Combine(RootConfigPath, "BedrockBoot.Log");
    public static readonly string UpdatePath = Path.Combine(RootConfigPath, "BedrockBoot.Update");
    public static readonly string TempPath = Path.Combine(RootConfigPath, "BedrockBoot.Temp");
    public static readonly string PluginPath = Path.Combine(RootConfigPath, "BedrockBoot.Plugin");
    public static readonly string GamePublicRootPath = Path.Combine(RootConfigPath, "BedrockBoot.GamePublic");

    public static List<OtherLauncherInfo> OtherLauncher = new()
    {
        new()
        {
            Name = "LeviLauncher",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/LeviLauncher.png",
            ConfigFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "levilauncher.exe",
                "config.json"
            ),
            OnImport = s =>
            {
                var conf = new ConfigEntity<ConfigLeviLauncher>(s, false);
                var realPath = Path.Combine(conf.Data.BaseRoot, "versions");
                var inPath = Path.Combine(conf.Data.BaseRoot, "bedrock_versions");
                if (!Directory.Exists(realPath) || 
                     Directory.Exists(inPath))
                    return;

                Directory.CreateSymbolicLink(inPath, realPath);
                GlobalModel.Config.Data.GameFolders.Add(new()
                {
                    GameFolderName = "LeviLauncher",
                    GameFolderPath = conf.Data.BaseRoot,
                });
                GlobalModel.Config.Save();
                
                GlobalModel.MainWindow.CloseDraw();
                DialogHost.Show(new()
                {
                    Title = "导入成功",
                    Content = "导入 LeviLauncher 启动器的配置成功",
                    CloseButtonText = "确定"
                });
            }
        }
    };
}