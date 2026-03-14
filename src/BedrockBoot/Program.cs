using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Core.Models;
using BedrockBoot.Models.Global;
using BedrockBoot.Win32;
using PaperConnect.Core.Module.Global;
using Round.SDK.Entity;
using Round.SDK.Enum;
using Round.SDK.Global;
using Round.SDK.Logger;
using Application = System.Windows.Forms.Application;

namespace BedrockBoot.Desktop;

internal sealed class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
    
    public static List<string> Args { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Args = args.ToList();
        GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
        GlobalModel.Config.Load();

        AppUpdater.ProcessStartupArgs(args);

        PluginEnvironment.RunningProduct = ProductEnum.BedrockBoot;
        EnvironmentLabel.ClientId = $"BedrockBoot {GlobalModel.BodyVersion}";

        if (GlobalModel.Config.Data.GatherInfo)
        {
            Task.Run(() => AnalyticsService.PushDeviceLog(GlobalModel.BodyVersion).ContinueWith(_ => { }));
        }

        ApplicationConfiguration.Initialize();
        if (args.Length > 0 && ArgsAnalytical(args.ToList()))
            return;

        if (GlobalModel.Config.Data.IsConsole)
        {
            AllocConsole();
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(@"已开启 Release 中的 Debug 模式，此模式不会生成日志！");
        }

        var consoleRedirector = new ConsoleRedirector(Path.Combine(PathsList.LogPath,
            $"[BedrockBoot.Logger] {DateTime.Now:yyyy.MM.dd HHmmss.fff}.log"));

        string bedrockBootLogo = $"""
                                  [######]  [######] [######]  [#####]   [#######] [#]  [#]    [######]  [#######] [#######] [#######]
                                  [#]   [#] [#]      [#]   [#] [#]   [#] [#]   [#] [#] [#]     [#]   [#] [#]   [#] [#]   [#]    [#]   
                                  [######]  [#####]  [#]   [#] [######]  [#]   [#] [####]      [######]  [#]   [#] [#]   [#]    [#]   
                                  [#]   [#] [#]      [#]   [#] [#]   [#] [#]   [#] [#] [#]     [#]   [#] [#]   [#] [#]   [#]    [#]   
                                  [######]  [######] [######]  [#]   [#] [#######] [#]  [#]    [######]  [#######] [#######]    [#]   

                                  >> BedrockBoot Ver.{GlobalModel.BodyVersion}
                                  ------------------------------------------------------------------------------------------------------
                                  """;

        Console.WriteLine(bedrockBootLogo);

        if ((int)GlobalModel.Config.Data.IsolationModel != 0)
            GlobalModel.Config.Data.IsolationModel = IsolationType.Hook;
        GlobalModel.Config.Save();

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

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static bool ArgsAnalytical(List<string> args)
    {
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-update":
                    Console.WriteLine(@"触发更新，本次启动将不会拉起窗体。");
                    AppUpdater.StartUpdateFromOldVersion(args[args.FindIndex(x => x == "-update") + 1]);
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

                case "-console":
                    AllocConsole();
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.WriteLine(@"已开启 Release 中的 Debug 模式，此模式不会生成日志！");
                    break;

                case "-jump":
                    Console.WriteLine(@"快捷启动");
                    args.ForEach(Console.WriteLine);

                    Application.Run(new LaunchWindow(args.ToList()));
                    return true;

                case "-open":
                    Console.WriteLine(@"导入资源");
                    args.ForEach(Console.WriteLine);

                    // Application.Run(new ImportResourcePack(args.ToList()));
                    GlobalModel.AppRunType = AppRunType.OpenResourcePack;

                    if (Args.Contains("--resource")) GlobalModel.AppRunType = AppRunType.OpenResourcePack;
                    if (Args.Contains("--world")) GlobalModel.AppRunType = AppRunType.OpenWorldPack;
                    
                    return false;
            }
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