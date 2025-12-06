using Avalonia;
using System;
using System.IO;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;
using Round.SDK.Enum;
using Round.SDK.Global;
using Round.SDK.Logger;

namespace BedrockBoot;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        PluginEnvironment.RunningProduct = ProductEnum.BedrockBoot;
        
        GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
        GlobalModel.Config.Load();
        
        ConsoleRedirector consoleRedirector = new ConsoleRedirector(Path.Combine(PathsList.LogPath,
            $"[BedrockBoot.Logger] {DateTime.Now.ToString("yyyy.MM.dd HHmmss.fff")}.log"));

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}