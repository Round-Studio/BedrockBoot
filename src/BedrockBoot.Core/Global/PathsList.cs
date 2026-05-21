using BedrockBoot.Base.Entry;

namespace BedrockBoot.Models.Global;

public class PathsList
{
    public static readonly string RootConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoundStudio",
            "BedrockBoot2");
    public static readonly string ConfigFolderPath = Path.Combine(RootConfigPath, "BedrockBoot.Config");

    public static readonly string ConfigPath = Path.Combine(ConfigFolderPath, "Config.json");
    public static readonly string MsAccountPath = Path.Combine(ConfigFolderPath, "account", "MsAccount.json");
    public static readonly string ProtonConfigPath = Path.Combine(RootConfigPath, "BedrockBoot.Config", "ProtonConfig.json");
    public static readonly string HistoryPath = Path.Combine(RootConfigPath, "BedrockBoot.Config", "SearchHistory.json");
    public static readonly string LogPath = Path.Combine(RootConfigPath, "BedrockBoot.Log");
    public static readonly string ProtonPath = Path.Combine(RootConfigPath, "BedrockBoot.Linux", "ProtonGDK");
    public static readonly string UpdatePath = Path.Combine(RootConfigPath, "BedrockBoot.Update");
    public static readonly string TempPath = Path.Combine(RootConfigPath, "BedrockBoot.Temp");
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

    public static List<OtherLauncherInfo> OtherLauncher = new()
    {
        /*new OtherLauncherInfo() // LeviLauncher
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
                if (!Directory.Exists(realPath)) Directory.CreateDirectory(realPath);

                if (!Directory.Exists(inPath)) Directory.CreateSymbolicLink(inPath, realPath);
                BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                {
                    GameFolderName = "LeviLauncher",
                    GameFolderPath = conf.Data.BaseRoot
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
        },
        new OtherLauncherInfo() // BMCBL
        {
            Name = "BMCBL",
            IconUrl = "avares://BedrockBoot/Assets/Icon/Other/BMCBL.png",
            IsExists = false,
            OnImport = async _ =>
            {
                var storageProvider = TopLevel.GetTopLevel(GlobalModel.MainWindow);
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
                        var inPath = Path.Combine(folder, "bedrock_versions");
                        if (!Directory.Exists(realPath) ||
                            Directory.Exists(inPath))
                        {
                            DialogHost.Show(new DialogInfo
                            {
                                Title = "提示",
                                Content = "该启动器已导入",
                                CloseButtonText = "确定"
                            });
                            return;
                        }

                        Directory.CreateSymbolicLink(inPath, realPath);
                        BedrockBoot.Core.Global.GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo
                        {
                            GameFolderName = "BMCBL",
                            GameFolderPath = folder
                        });
                        BedrockBoot.Core.Global.GlobalModel.Config.Save();

                        GlobalModel.MainWindow.CloseDraw();
                        DialogHost.Show(new DialogInfo
                        {
                            Title = "导入成功",
                            Content = "导入 BMCBL 启动器的配置成功",
                            CloseButtonText = "确定"
                        });
                    }
                }
            }
        }*/
    };
}