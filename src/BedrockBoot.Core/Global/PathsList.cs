using BedrockBoot.Base.Entry;

namespace BedrockBoot.Models.Global;

public class PathsList
{
    public static readonly string RootConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2");
    public static readonly string ConfigFolderPath = Path.Combine(RootConfigPath, "BedrockBoot.Config");

    public static readonly string ConfigPath = Path.Combine(RootConfigPath, "BedrockBoot.Config", "Config.json");

    public static readonly string ProtonConfigPath =
        Path.Combine(RootConfigPath, "BedrockBoot.Config", "ProtonConfig.json");

    public static readonly string WidgetsConfigPath =
        Path.Combine(RootConfigPath, "BedrockBoot.Config", "WidgetsConfig.json");

    public static readonly string
        HistoryPath = Path.Combine(RootConfigPath, "BedrockBoot.Config", "SearchHistory.json");
    public static readonly string MsAccountPath = Path.Combine(ConfigFolderPath, "account", "MsAccounts.json");
    public static readonly string LogPath = Path.Combine(RootConfigPath, "BedrockBoot.Log");
    public static readonly string ProtonPath = Path.Combine(RootConfigPath, "BedrockBoot.Linux", "ProtonGDK");
    public static readonly string PreFixPath = Path.Combine(ProtonPath, "game_prefix");
    public static readonly string UpdatePath = Path.Combine(RootConfigPath, "BedrockBoot.Update");
    public static readonly string TempPath = Path.Combine(RootConfigPath, "BedrockBoot.Temp");
    public static readonly string ThemePath = Path.Combine(RootConfigPath, "BedrockBoot.Theme", "packs");
    public static readonly string PluginPath = Path.Combine(RootConfigPath, "BedrockBoot.Plugin");
    public static readonly string GamePublicRootPath = Path.Combine(RootConfigPath, "BedrockBoot.GamePublic");
    public static readonly string GameBackup = Path.Combine(RootConfigPath, "BedrockBoot.GameBackup");
    public static readonly string ArchiveBackup = Path.Combine(GameBackup, "archive_backup");
    public static readonly string ReportPath = Path.Combine(RootConfigPath, "BedrockBoot.ErrorReport");
    public static readonly string PaperConnectPath = Path.Combine(RootConfigPath, "BedrockBoot.PaperConnect");

    public static readonly string EasyTierPath = Path.Combine(PaperConnectPath, "EasyTier");

    public static readonly string EasyTierCorePath =
        Path.Combine(PaperConnectPath, "EasyTier", "easytier-windows-x86_64", "easytier-core.exe");

    public static readonly string EasyTierCliPath =
        Path.Combine(PaperConnectPath, "EasyTier", "easytier-windows-x86_64", "easytier-cli.exe");

    public static readonly string PreauthDir = Path.Combine(ConfigFolderPath, "preauth");
    public static readonly string DeviceJsonPath = Path.Combine(PreauthDir, "device.json");
    public static readonly string DeviceKeyPath = Path.Combine(PreauthDir, "device-key.pem");
    public static readonly string DeviceIdPath = Path.Combine(PreauthDir, "device-id.txt");
    public const string WinegdkReg = @"Software\Wine\WineGDK";
}