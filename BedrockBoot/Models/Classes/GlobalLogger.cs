namespace BedrockBoot.Models.Classes;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

public static class GlobalLogger
{
    private static ILogger _logger;
    private static bool _isInitialized = false;

    /// <summary>
    /// 初始化Serilog日志系统
    /// </summary>
    public static void Initialize(string logDirectory = null)
    {
        if (_isInitialized) return;

        logDirectory ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "application-.log");

        // 配置Serilog
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithExceptionDetails() // 详细异常信息
            .WriteTo.Async(a => a.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ProcessId}/{ThreadId}] {Message:lj}{NewLine}{Exception}{NewLine}{Properties}{NewLine}"
            ))
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        // 注册全局异常处理
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // 设置全局Logger
        Log.Logger = _logger;
        _isInitialized = true;

        _logger.Information("全局日志系统初始化完成");
    }

    /// <summary>
    /// 获取Logger实例
    /// </summary>
    public static ILogger Logger => _logger ?? throw new InvalidOperationException("日志系统未初始化");

    /// <summary>
    /// 应用程序域未处理异常
    /// </summary>
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is System.Exception ex)
        {
            _logger.Fatal(ex, "应用程序发生未处理异常，应用程序将终止: {IsTerminating}", e.IsTerminating);
        }
        else
        {
            _logger.Fatal("应用程序发生未知未处理异常，应用程序将终止: {IsTerminating}", e.IsTerminating);
        }

        // 确保日志被刷新
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Task未观察到的异常
    /// </summary>
    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "Task发生未观察到的异常");
        e.SetObserved(); // 标记为已处理，防止应用程序崩溃
    }

    /// <summary>
    /// 应用程序退出
    /// </summary>
    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        _logger.Information("应用程序正在退出");
        Log.CloseAndFlush();
    }

    /// <summary>
    /// 记录崩溃日志
    /// </summary>
    public static void LogCrash(System.Exception exception, string context = null)
    {
        _logger.Fatal(exception, "应用程序崩溃: {Context}", context);

        // 立即刷新日志以确保写入
        (Log.Logger as Serilog.Core.Logger)?.Dispose();
    }
}