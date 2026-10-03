using System.Diagnostics;
using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Standard.Interface.Platform.Game;

public abstract class ILauncher
{
    protected ILauncher(VersionConfig options)
    {
        VersionInfo = options;
    }

    public abstract Task LaunchGame();

    protected VersionConfig VersionInfo { get; set; }
    public Action? OnMigration { get; set; }
    public Action<Process>? Launched { get; set; }
    public Action? LaunchCompleted { get; set; }
    public Action<string, double>? UpdateProgress { get; set; }
    public Action<string>? UpdateProgressText { get; set; }
    public Action<bool>? SetProgressIndeterminate { get; set; }
    public Process MinecraftProcess { get; set; }
    public static Action? LaunchedBehavior { get; set; }
    public static Action<VersionConfig>? OnGameLaunched { get; set; }
    public static Action<VersionConfig>? OnGameExited { get; set; }
}