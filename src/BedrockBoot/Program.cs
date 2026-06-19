using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core;
using BedrockBoot.Core.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Service.Protocol;
using PaperConnect.Core.Module.Global;
using Round.SDK.Entity;
using Round.SDK.Enum;
using Round.SDK.Global;
using Round.SDK.Logger;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;
#if WINDOWS
using System.Windows.Forms;
using BedrockBoot.Models.Helper.Uwp;
using BedrockBoot.Win32;
using Application = System.Windows.Forms.Application;
#endif

namespace BedrockBoot.Desktop;

internal sealed class Program
{
    public static List<string> Args { get; private set; }

#if WINDOWS
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
#endif

    [STAThread]
    public static void Main(string[] args)
    {
        Args = args.ToList();

#if WINDOWS
        BedrockbootProtocolRegistration.Register();
#endif

        GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
        GlobalModel.Config.Load();

        AppUpdater.ProcessStartupArgs(args);

        PluginEnvironment.RunningProduct = ProductEnum.BedrockBoot;
        EnvironmentLabel.ClientId = $"BedrockBoot {Models.Global.GlobalModel.BodyVersion}";

        if (GlobalModel.Config.Data.GatherInfo)
            Task.Run(() =>
                AnalyticsService.PushDeviceLog(Models.Global.GlobalModel.BodyVersion).ContinueWith(_ => { }));

#if WINDOWS
        ApplicationConfiguration.Initialize();
#endif
        if (args.Length > 0 && ArgsAnalytical(args.ToList()))
            return;

#if WINDOWS
        if (GlobalModel.Config.Data.IsConsole)
        {
            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(@"已开启 Release 中的 Debug 模式，此模式不会生成日志！");
        }
#endif

        var consoleRedirector = new ConsoleRedirector(Path.Combine(PathsList.LogPath,
            $"[BedrockBoot.Logger] {DateTime.Now:yyyy.MM.dd HHmmss.fff}.log"));
        
        Console.WriteLine(@"日志模块初始化完毕");

        var bedrockBootLogo = $"""
                                 ____           _                 _     ____              _
                                | __ )  ___  __| |_ __ ___   ___ | | __| __ )  ___   ___ | |_
                                |  _ \ / _ \/ _` | '__/ _ \ / _ \| |/ /|  _ \ / _ \ / _ \| __|
                                | |_) |  __/ (_| | | | (_) | (_) |   < | |_) | (_) | (_) | |_
                                |____/ \___|\__,_|_|  \___/ \___/|_|\_\|____/ \___/ \___/ \__|
                               >> BedrockBoot Ver.{Models.Global.GlobalModel.BodyVersion}
                               -----------------------------------------------------------------------
                               """;

        Console.WriteLine(bedrockBootLogo);

        if ((int)GlobalModel.Config.Data.IsolationModel != 0)
            GlobalModel.Config.Data.IsolationModel = IsolationType.Hook;
        GlobalModel.Config.Save();

#if WINDOWS
        if (!VCRedistDetector.CheckInInstalledList().IsInstalled)
        {
            var dialog = MessageBox.Show(
                new StringBuilder()
                    .AppendLine("当前用户尚未安装 Microsoft Visual C++ 2015-2022 Redistributable 运行库，可能会导致启动器或游戏无法正常运行。")
                    .AppendLine("")
                    .AppendLine("[OK] 自动安装 VC 2015-2022")
                    .AppendLine("[Cancel] 继续运行启动器（忽略运行库问题）")
                    .AppendLine("")
                    .AppendLine("详情请参见")
                    .AppendLine("https://docs.roundstudio.top/docs/product/bb/commonQuestion")
                    .ToString(), @"BedrockBoot 警告", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

            if (dialog == DialogResult.OK)
            {
                Application.Run(new DownloadVCWindow());

                return;
            }
        }
#endif

#if WINDOWS
        if (UwpDependencyChecker.GetMissingDependencies().Count > 0 &&
            !args.Contains("-runas"))
        {
            RebootWithRunas();
        }
#endif

        var appArgs = args.Where(a => a != "-bedrockboot" && !a.StartsWith("bedrockboot:", StringComparison.OrdinalIgnoreCase)).ToArray();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(appArgs);
    }

    private static void RebootWithRunas()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName 
                      ?? throw new InvalidOperationException("无法获取可执行文件路径");
    
        var workingDir = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;
        var args = "-runas";
    
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = true,
            CreateNoWindow = false,
            Verb = "runas"
        };
                
        Process.Start(startInfo);
        Environment.Exit(0);
    }

    private static bool ArgsAnalytical(List<string> args)
    {
        foreach (var arg in args)
            switch (arg)
            {
                case "-update":
                    Console.WriteLine(@"触发更新，本次启动将不会拉起窗体。");
                    AppUpdater.StartUpdateFromDownloadedFile(args[args.FindIndex(x => x == "-update") + 1]);
                    return true;

                case "-shell":
                    var shellIndex = args.FindIndex(x => x == "-shell");
                    if (shellIndex + 1 >= args.Count)
                    {
                        Console.WriteLine(@"错误：-shell 参数后需要指定命令");
                        break;
                    }

                    var command = args[shellIndex + 1];
                    Console.WriteLine($@"触发 bb 协议：{command}");

                    try
                    {
                        Sent(command);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"请求出错：{ex.Message}");
                        Task.Run(async () =>
                        {
                            await Task.Delay(2000);
                            Sent(command);
                        });
                        return true;
                    }

#if WINDOWS
                case "-console":
                    AllocConsole();
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.WriteLine(@"已开启 Release 中的 Debug 模式，此模式不会生成日志！");
                    break;
#endif

                case "-jump":
                    Console.WriteLine(@"快捷启动");
                    args.ForEach(Console.WriteLine);

#if WINDOWS
                    Application.Run(new LaunchWindow(args.ToList()));
#endif
                    return true;

                case "-bedrockboot":
                    var protoIndex = args.FindIndex(x => x == "-bedrockboot");
                    if (protoIndex + 1 >= args.Count)
                    {
                        Console.WriteLine(@"错误：-bedrockboot 参数后需要指定协议 URL");
                        break;
                    }

                    var protocolUrl = args[protoIndex + 1];
                    Console.WriteLine($@"收到 BedrockBoot 协议请求: {protocolUrl}");

                    var parsedCommand = BedrockbootProtocolHandler.ParseProtocolUrl(protocolUrl);
                    if (parsedCommand == null)
                    {
                        Console.WriteLine($@"无法解析协议 URL: {protocolUrl}");
                        break;
                    }

                    for (var retry = 0; retry < 5; retry++)
                    {
                        if (BedrockbootProtocolHandler.TrySendCommand(parsedCommand))
                        {
                            Console.WriteLine(@"已转发至运行中的主窗口进程");
                            return true;
                        }
                        Thread.Sleep(300);
                    }

                    Console.WriteLine(@"未检测到运行中的主窗口，将在当前进程启动");
                    BedrockbootProtocolHandler.PendingCommand = parsedCommand;
                    return false;

                case "-open":
                    Console.WriteLine(@"导入资源");
                    args.ForEach(Console.WriteLine);

                    // Application.Run(new ImportResourcePack(args.ToList()));
                    Models.Global.GlobalModel.AppRunType = AppRunType.OpenResourcePack;

                    if (Args.Contains("--resource")) Models.Global.GlobalModel.AppRunType = AppRunType.OpenResourcePack;
                    if (Args.Contains("--world")) Models.Global.GlobalModel.AppRunType = AppRunType.OpenWorldPack;

                    return false;
            }

        return false;
    }

    private static void Sent(string command)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        using var httpClient = new HttpClient(handler);

        var url = $"http://127.0.0.1:43956/shell?command={Uri.EscapeDataString(command)}";
        var response = httpClient.GetAsync(url).Result;

        if (response.IsSuccessStatusCode)
        {
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($@"服务器响应：{responseContent}");
        }
        else
        {
            Console.WriteLine($@"请求失败，状态码：{response.StatusCode}");
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
