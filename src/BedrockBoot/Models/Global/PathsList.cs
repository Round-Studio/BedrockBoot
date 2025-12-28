using System;
using System.IO;

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
}