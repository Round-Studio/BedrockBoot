using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Base.Helper;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Style;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Windows;
using BedrockBoot.Views.Windows.SystemMethod;
using BedrockBoot.WatchDog.Entity;
using OnePointUI.Avalonia.Style.Core;
using Round.SDK.Entity;
using Application = Avalonia.Application;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;
using Window = Avalonia.Controls.Window;

namespace BedrockBoot;

public class App : Application
{
    public override void Initialize()
    {
        if (GlobalModel.Config == null)
        {
            GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
            GlobalModel.Config.Load();
        }

        ServicePointManager.DefaultConnectionLimit = 1024;

        ThemeManager.Initialize(this);
        AvaloniaXamlLoader.Load(this);

        I18nManager.Instance.SystemLanguage(GlobalModel.Config.Data.Language);

        // 订阅所有全局异常处理器
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;

        Console.WriteLine(@"异常订阅已完毕");

        var watchDog = new WatchDog.WatchDog(new WatchConfig());
        watchDog.Start();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogException(exception, "AppDomain");
        ShowErrorDialog(exception);

        // 非致命错误可以继续运行
        if (!e.IsTerminating)
        {
        }
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "Task");
        ShowErrorDialog(e.Exception);
        e.SetObserved(); // 标记为已处理
    }

    private void ShowErrorDialog(Exception ex)
    {
        try
        {
            if (!Directory.Exists(PathsList.ReportPath)) Directory.CreateDirectory(PathsList.ReportPath);
            var errorReportJson = ErrorReport.Create(GlobalModel.Config.Data, "错误报告", ex);
            errorReportJson.SaveToFile(Path.Combine(PathsList.ReportPath,
                DateTime.Now.ToString("yyyyMMddHHmmss") + ".json"));
            Dispatcher.UIThread.Invoke(() => { new ExceptionWindow(errorReportJson).Show(); });
        }
        catch (Exception dialogEx)
        {
            // 如果对话框本身出错，至少记录到控制台
            Console.WriteLine($@"显示错误对话框时出错: {dialogEx}");
        }
    }

    private void LogException(Exception ex, string source)
    {
        Console.WriteLine($@"{source} 异常: {ex?.ToString() ?? "未知异常"}");
    }

    private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "UI Thread");
        ShowErrorDialog(e.Exception);
        e.Handled = true; // 阻止应用崩溃
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avalonia 12 removed BindingPlugins.DataValidators; the data annotations
            // validation plugin is disabled by default and no longer needs to be removed.

            Window window = null;

            Core.Global.GlobalModel.Config.AddAfterSaveCallback(entity =>
            {
                IsolationPolicyHelper.PublicCatalogStrategy = Core.Global.GlobalModel.Config.Data.CatalogStrategy;
            });
            IsolationPolicyHelper.PublicCatalogStrategy = Core.Global.GlobalModel.Config.Data.CatalogStrategy;

            switch (Models.Global.GlobalModel.AppRunType)
            {
                case AppRunType.Default:
                    window = new MainWindow();
                    // StartProtocolServer();
                    Console.WriteLine(@"暂时先不启用 IPC 服务器了...");
                    break;
                case AppRunType.OpenResourcePack:
                    window = new ImportResourcePack();
                    break;
                case AppRunType.OpenWorldPack:
                    window = new ImportWorldPack();
                    break;
                case AppRunType.OpenTemplatePack:
                    window = new ImportTemplatePack();
                    break;
                case AppRunType.LaunchGame:
                    window = new LaunchGameWindow();
                    break;
            }

            if (window == null) throw new NullReferenceException();

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartProtocolServer()
    {
        try
        {
            var protoService = Models.Global.GlobalModel.ProtocolService;

            protoService.Get("/shell", async (context, parameters) =>
            {
                var command = parameters.GetQuery("command");
                if (!string.IsNullOrEmpty(command))
                {
                    Dispatcher.UIThread.Post(() => ProtocolCommand.OnCommand([command]));
                    await ProtocolService.WriteTextResponseAsync(context, 200, "ok");
                }
                else
                {
                    await ProtocolService.WriteErrorResponseAsync(context, 400, "Bad Request",
                        "Missing command parameter");
                }
            });

            _ = protoService.StartAsync();
            Console.WriteLine(@"BedrockBoot IPC 服务已启动");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"启动 BedrockBoot IPC 服务失败: {ex.Message}");
        }
    }

    public static void LoadColor(string colorCode,ThemeModelEnum theme)
    {
        try
        {
            ThemeManager.Instance.SetAccentColor(
                Color.Parse(colorCode));
            ThemeManager.Instance.SetThemeModel(
                theme == ThemeModelEnum.Light
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark);
        }
        catch
        {
        }
    }
}