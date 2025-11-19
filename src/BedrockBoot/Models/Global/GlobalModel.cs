using BedrockBoot.Base.Entry;
using BedrockBoot.Views.Windows;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class GlobalModel
{
    public static ConfigEntity<ConfigEntry> Config;
    public static MainWindow MainWindow;
    public static BedrockCore BedrockCore { get; set; }
}