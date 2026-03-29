using System.Collections.Generic;
using System.Reflection;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Info.Xbox;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Windows;
using BedrockLauncher.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class GlobalModel
{
    public static ConfigEntity<ConfigEntry> Config;
    public static MainWindow MainWindow;
    public static BedrockCore BedrockCore { get; set; }
    public static TaskManager TaskManager { get; set; } = new();
    public static bool IsAbleToLaunchGame { get; set; } = false;
    public static FunctionOptionEntry FunctionOption { get; set; }
    public static string BodyVersion => $"{Assembly.GetExecutingAssembly().GetName().Version!.ToString()}-{CheckUpdate.GetBodyUpdateType()}";
    public static ProtocolService ProtocolService { get; set; } = new();
    public static ImageLoader ImageLoader { get; set; } = new();
    public static PaperConnectCore PaperConnectCore { get; set; }
    public static List<string> ETPublicServer { get; set; }
    public static XboxUserInfo XboxUserInfo { get; set; }
    public static AppRunType AppRunType { get; set; } = AppRunType.Default;
    public static bool IsNetworkAvailable { get; set; }
    public static ArchiveBackup ArchiveBackup { get; } = new();
}