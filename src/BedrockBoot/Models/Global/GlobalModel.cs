using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Config;
using BedrockBoot.Base.Entry.Info.Xbox;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.GravityCone;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.Windows;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Global;

public class GlobalModel
{
    public static GravityConeClient? GravityConeClient { get; set; } = null;
    public static RoomState? CurrentRoomState { get; set; } = null;
    public static MainWindow MainWindow;
    public static TaskManager TaskManager { get; set; } = new();
    public static bool IsAbleToLaunchGame { get; set; } = false;
    public static bool IsProgressRunning { get; set; } = false;
    public static FunctionOptionEntry FunctionOption { get; set; }
    public static CustomManifest CustomManifest { get; set; } = new();

    public static string BodyVersion =>
        $"{Assembly.GetExecutingAssembly().GetName().Version!.ToString()}-{CheckUpdate.GetBodyUpdateType()}";
    
    public static Action? MainPageUpdateInstance { get; set; }

    public static PaperConnectCore PaperConnectCore { get; set; }
    public static List<string> ETPublicServer { get; set; }
    public static XboxUserInfo XboxUserInfo { get; set; }
    public static AppRunType AppRunType { get; set; } = AppRunType.Default;
    public static bool IsNetworkAvailable { get; set; }
    public static ArchiveBackup ArchiveBackup { get; } = new();
    public static List<OtherLauncherInfo> OtherLauncher = new()
    {
        new OtherLauncherInfo() // BMCBL
        {
            Name = "BMCBL",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/BMCBL.png",
            IsExists = false,
            OnImport = async _ =>
            {
                var storageProvider = TopLevel.GetTopLevel(GlobalModel.MainWindow);
                if (storageProvider == null)
                {
                    Console.WriteLine(@"无法获取主窗口的顶层存储提供程序");
                    return;
                }

                var options = new FilePickerOpenOptions
                {
                    Title = "选择 BMCBL 本体",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("EXE 可执行文件")
                        {
                            Patterns = new[] { "*.exe" }
                        }
                    }
                };

                var files = await storageProvider.StorageProvider.OpenFilePickerAsync(options);
                if (files != null && files.Count > 0)
                {
                    var selectedFile = files[0];

                    var filePath = selectedFile.Path.LocalPath;
                    var folder = Path.Combine(Path.GetDirectoryName(filePath), "BMCBL");
                    var realPath = Path.Combine(folder, "versions");
                    if (Directory.Exists(folder))
                    {
                        /*var inPath = Path.Combine(folder, GameInfoHelper.GetGameFolderRootName(currentFolder.GameFolderPath));
                        if (Directory.Exists(realPath) &&
                            !Directory.Exists(inPath))
                        {
                            Directory.CreateSymbolicLink(inPath, realPath);
                        }*/

                        BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                        {
                            GameFolderName = "BMCBL",
                            GameFolderPath = folder,
                            GameFolderType = GameFolderType.BMCBL
                        });
                        BedrockBoot.Core.Global.GlobalModel.Config.Save();

                        MainWindow?.CloseDraw();
                        DialogHost.Show(new DialogInfo
                        {
                            Title = "导入成功",
                            Content = "导入 BMCBL 启动器的配置成功",
                            CloseButtonText = "确定"
                        });
                    }
                }
            }
        },
        new OtherLauncherInfo() // LeviLauncher
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
                /*var realPath = Path.Combine(conf.Data.BaseRoot, "versions");
                var inPath = Path.Combine(conf.Data.BaseRoot, GameInfoHelper.GetGameFolderRootName(currentFolder.GameFolderPath));
                if (!Directory.Exists(realPath)) Directory.CreateDirectory(realPath);

                if (!Directory.Exists(inPath)) Directory.CreateSymbolicLink(inPath, realPath);*/
                BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                {
                    GameFolderName = "LeviLauncher",
                    GameFolderPath = conf.Data.BaseRoot,
                    GameFolderType = GameFolderType.LeviLauncher
                });
                BedrockBoot.Core.Global.GlobalModel.Config.Save();

                GlobalModel.MainWindow.CloseDraw();
                DialogHost.Show(new DialogInfo
                {
                    Title = "导入成功",
                    Content = "导入 LeviLauncher 启动器的配置成功",
                    CloseButtonText = "确定"
                });
            }
        }
    };
}