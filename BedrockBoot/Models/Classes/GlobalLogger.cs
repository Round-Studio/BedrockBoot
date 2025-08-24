namespace BedrockBoot.Models.Classes;

using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

public static class GlobalLogger
{
    private static ILogger _logger;
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();

    /// <summary>
    /// 初始化Serilog日志系统
    /// </summary>
    public static void Initialize(string logDirectory = null)
    {
        lock (_initLock)
        {
            if (_isInitialized) return;

            logDirectory ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            var logFilePath = Path.Combine(logDirectory, "application-.log");

            try
            {
                // 配置Serilog
                _logger = new LoggerConfiguration()
                    .MinimumLevel.Debug() // 设置最低日志级别
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 第三方库日志级别
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.WithExceptionDetails() // 详细异常信息
                    .WriteTo.Async(a => a.File( // 异步文件输出
                        path: logFilePath,
                        rollingInterval: RollingInterval.Day, // 按天分割日志
                        retainedFileCountLimit: 30, // 保留30天日志
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{ProcessId}/{ThreadId}] {Message:lj}{NewLine}{Exception}{NewLine}"
                    ))
                    .WriteTo.Console( // 控制台输出
                        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                    )
                    .CreateLogger();

                // 注册全局异常处理
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

                // 设置为全局Logger
                Log.Logger = _logger;
                _isInitialized = true;

                _logger.Information("全局日志系统初始化完成，日志目录: {LogDirectory}", logDirectory);
            }
            catch (System.Exception ex)
            {
                // 如果初始化失败，至少创建一个基本的logger
                _logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(logDirectory, "fallback.log"))
                    .CreateLogger();

                _logger.Error(ex, "日志系统初始化失败，使用回退模式");
            }
        }
    }

    /// <summary>
    /// 获取Logger实例
    /// </summary>
    public static ILogger Logger
    {
        get
        {
            if (!_isInitialized)
            {
                // 如果没有初始化，自动初始化一个基本配置
                Initialize();
            }
            return _logger;
        }
    }

    // 全局异常处理事件
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

        FlushAndClose();
    }

    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "Task发生未观察到的异常");
        e.SetObserved(); // 标记为已处理
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        _logger.Information("应用程序进程退出");
        FlushAndClose();
    }

    private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
    {
        _logger.Information("应用程序域卸载");
        FlushAndClose();
    }

    /// <summary>
    /// 刷新并关闭日志
    /// </summary>
    private static void FlushAndClose()
    {
        try
        {
            Log.CloseAndFlush();
        }
        catch
        {
            // 忽略关闭错误
        }
    }

    /// <summary>
    /// 记录崩溃日志
    /// </summary>
    public static void LogCrash(System.Exception exception, string context = null)
    {
        _logger.Fatal(exception, "应用程序崩溃: {Context}", context);
        FlushAndClose();
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public static void Information(string message, params object[] properties)
    {
        Logger.Information(message, properties);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void Error(System.Exception exception, string message, params object[] properties)
    {
        Logger.Error(exception, message, properties);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void Warning(string message, params object[] properties)
    {
        Logger.Warning(message, properties);
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public static void Debug(string message, params object[] properties)
    {
        Logger.Debug(message, properties);
    }
}