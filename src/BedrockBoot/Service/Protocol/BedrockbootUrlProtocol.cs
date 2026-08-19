using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Service.Protocol.Routes;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace BedrockBoot.Service.Protocol;

/// <summary>
///     BedrockBoot 自定义 URL 协议（bedrockboot://）的统一入口。
///     职责：注册/卸载协议、解析协议 URL、保证全局单实例、
///     并通过命名管道把协议请求转发给已经运行的实例。
///     单实例标识与 exe 名称无关（基于固定的产品级名称），
///     因此不同 exe 名称（如改名/开发版）之间也能互相兼容单实例。
/// </summary>
public static class BedrockbootUrlProtocol
{
    /// <summary>
    ///     协议名称（浏览器中表现为 bedrockboot://）
    /// </summary>
    public const string Scheme = "bedrockboot";

    /// <summary>
    ///     注册表命令行开关，浏览器打开协议时以此参数拉起本程序
    /// </summary>
    public const string CliSwitch = "-bedrockboot";

    /// <summary>
    ///     兼容旧版本注册表残留的 -shell 开关
    /// </summary>
    private const string LegacyCliSwitch = "-shell";

    private const string ProtocolDescription = "BedrockBoot - Minecraft 基岩版启动器";

    /// <summary>
    ///     Windows 11/10 默认应用关联的固定注册表位置。
    ///     所有（无论 name 是否为 BedrockBoot.exe）进程都写入同一位置，
    ///     以便改名后的进程能互相接管协议关联。
    /// </summary>
#if WINDOWS
    private const string CapabilitiesRegistryPath = @"Software\RoundStudio\BedrockBoot\Capabilities";
    private const string RegisteredApplicationsPath = @"Software\RegisteredApplications";
#endif

    /// <summary>
    ///     命名管道名称：与 exe 名称无关，改名前缀的多个进程共用同一条管道
    /// </summary>
    private const string PipeName = @"RoundStudio.BedrockBoot.UrlProtocol";

    /// <summary>
    ///     单例互斥体名称：同样与 exe 名称无关。
    ///     使用 Local\ 前缀保证按 Windows 会话隔离，配合当前用户限定管道，避免跨会话/跨用户串扰。
    /// </summary>
    private const string MutexNameWindows = @"Local\RoundStudio.BedrockBoot.UrlProtocol";
    private const string MutexNameUnix = @"RoundStudio.BedrockBoot.UrlProtocol";

    /// <summary>
    ///     转发协议请求到已运行实例的总预算时间
    /// </summary>
    private static readonly TimeSpan ForwardDeadline = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     单次尝试连接管道的最长等待时间
    /// </summary>
    private static readonly TimeSpan ConnectAttemptTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     服务端等待单个请求的最长时间（防止异常客户端长时间占用唯一管道实例）
    /// </summary>
    private static readonly TimeSpan IpcReadTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     单条协议请求的最大长度，防止内存被异常数据撑爆
    /// </summary>
    private const int MaxPayloadLength = 64 * 1024;

    private static readonly CancellationTokenSource IpcShutdown = new();

    /// <summary>
    ///     持有对互斥体的引用，保证整个进程生命周期内不被回收、不被释放
    /// </summary>
    private static Mutex _instanceMutex;

    /// <summary>
    ///     本实例启动时携带的协议请求（若本实例是唯一实例则暂存，待 UI 就绪后执行）
    /// </summary>
    private static ProtocolRequest _pendingRequest;

    /// <summary>
    ///     取走并清空暂存的协议请求
    /// </summary>
    public static ProtocolRequest? TakePendingRequest()
    {
        var request = _pendingRequest;
        _pendingRequest = null;
        return request;
    }

    /// <summary>
    ///     启动阶段调用：保证只唤醒一个进程实例，并处理协议唤醒。
    /// </summary>
    /// <param name="args">进程命令行参数</param>
    /// <returns>true 表示本进程应立即退出；false 表示继续正常启动</returns>
    public static bool WakeUp(IReadOnlyList<string> args)
    {
        if (HasSpecialMode(args))
        {
            Console.WriteLine(@"更新/提权/文件导入/快捷启动模式，跳过单实例限制");
            return false;
        }

        var payload = ExtractProtocolPayload(args);

        // 每次启动都重写注册表指向当前 exe，避免改名/更新后协议失效
        Register();

        if (!TryAcquireFirstInstance())
        {
            Console.WriteLine(@"检测到已有 BedrockBoot 实例在运行");
            if (string.IsNullOrEmpty(payload))
            {
                Console.WriteLine(@"本进程退出，保证仅保留一个实例");
                return true;
            }

            var forwarded = TryForwardAsync(payload).GetAwaiter().GetResult();
            Console.WriteLine(forwarded
                ? @"协议请求已转发至运行中的实例，本进程退出"
                : @"转发协议请求失败，本进程退出");
            return true;
        }

        Console.WriteLine(@"本进程作为唯一 BedrockBoot 实例继续运行");

        // 成为唯一实例后立刻启动 IPC 服务，缩小协议唤醒的竞争窗口
        _ = ListenAsync(IpcShutdown.Token);

        ProtocolRouteRegistry.Instance.RegisterRange(new IProtocolRoute[] { new AboutProtocolRoute() });

        if (!string.IsNullOrEmpty(payload))
        {
            var request = Parse(payload);
            if (request == null)
            {
                Console.WriteLine($@"无法解析协议 URL: {payload}");
            }
            else
            {
                _pendingRequest = request;
                Console.WriteLine($@"暂存协议请求，待界面就绪后执行: {payload}");
            }
        }

        return false;
    }

    /// <summary>
    ///     解析 bedrockboot:// URL
    /// </summary>
    /// <param name="url">原始协议 URL，如 bedrockboot://about?from=home</param>
    /// <returns>解析出的协议请求；不是本协议时返回 null</returns>
    public static ProtocolRequest? Parse(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var text = url.Trim();
        var rest = ExtractRemainderAfterScheme(text);
        if (rest == null)
            return null;

        // 去掉 fragment，浏览器可能追加 #hash
        var fragmentIndex = rest.IndexOf('#');
        if (fragmentIndex >= 0)
            rest = rest[..fragmentIndex];

        // 分离 path 与 query
        var queryIndex = rest.IndexOf('?');
        var path = queryIndex >= 0 ? rest[..queryIndex] : rest;
        var queryText = queryIndex >= 0 ? rest[(queryIndex + 1)..] : "";

        var pathParts = path.Trim('/').Split('/');
        var route = pathParts.Length > 0 && pathParts[0].Length > 0 ? pathParts[0] : "";

        var segments = new List<string>();
        for (var i = 1; i < pathParts.Length; i++)
            if (pathParts[i].Length > 0)
                segments.Add(Uri.UnescapeDataString(pathParts[i]));

        return new ProtocolRequest
        {
            Raw = url,
            Route = route,
            Segments = segments.ToArray(),
            Query = ParseQueryString(queryText)
        };
    }

    /// <summary>
    ///     执行协议请求（会切换到 UI 线程执行路由）
    /// </summary>
    public static void Dispatch(ProtocolRequest request)
    {
        if (string.IsNullOrEmpty(request.Route))
        {
            Console.WriteLine(@"协议请求未携带路由");
            return;
        }

        var route = ProtocolRouteRegistry.Instance.Get(request.Route);
        if (route == null)
        {
            Console.WriteLine($@"未注册的路由: {Scheme}://{request.Route}");
            Console.WriteLine($@"已注册的路由: {string.Join(", ", ProtocolRouteRegistry.Instance.GetRegisteredRouteNames())}");
            return;
        }

        var target = request.Segments.Length > 0 ? string.Join("/", request.Segments) : "";
        Console.WriteLine($@"执行协议路由: {Scheme}://{request.Route}/{(target.Length > 0 ? target : "")}");
        Dispatcher.UIThread.Post(() => { _ = route.ExecuteAsync(request.Segments, request.Query); });
    }

    /// <summary>
    ///     执行暂存的协议请求（UI 就绪后调用）
    /// </summary>
    public static void ExecutePendingRequest()
    {
        var request = TakePendingRequest();
        if (request == null)
            return;

        Console.WriteLine($@"执行暂存的协议请求: {request.Raw}");
        Dispatch(request);
    }

    /// <summary>
    ///     过滤掉协议相关参数，避免干扰 Avalonia 等后续参数解析
    /// </summary>
    public static IReadOnlyList<string> FilterStartupArgs(IReadOnlyList<string> args)
    {
        var result = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg == CliSwitch || arg == LegacyCliSwitch)
            {
                i++; // 同时跳过紧随其后的协议 URL
                continue;
            }

            if (arg.StartsWith(Scheme + ":", StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(arg);
        }

        return result;
    }

    /// <summary>
    ///     注册协议到注册表（仅 Windows，幂等，可在每次启动时调用以自修复）
    /// </summary>
    public static void Register()
    {
#if WINDOWS
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                Console.WriteLine(@"无法获取可执行文件路径，协议注册失败");
                return;
            }

            var protocolRoot = $@"Software\Classes\{Scheme}";

            using (var key = Registry.CurrentUser.CreateSubKey(protocolRoot))
            {
                key.SetValue("", ProtocolDescription);
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    defaultIcon.SetValue("", $"\"{exePath}\",0");

                using (var command = key.CreateSubKey(@"shell\open\command"))
                    command.SetValue("", $"\"{exePath}\" {CliSwitch} \"%1\"");
            }

            // Windows 10/11 默认应用关联（固定位置，多个 exe 名互相接管）
            using (var capabilities = Registry.CurrentUser.CreateSubKey(CapabilitiesRegistryPath))
            {
                capabilities.SetValue("ApplicationName", ProtocolDescription);
                capabilities.SetValue("ApplicationDescription", ProtocolDescription);

                using (var associations = capabilities.CreateSubKey("URLAssociations"))
                    associations.SetValue(Scheme, ProtocolDescription);
            }

            using (var registeredApps = Registry.CurrentUser.CreateSubKey(RegisteredApplicationsPath))
                registeredApps.SetValue(ProtocolDescription, CapabilitiesRegistryPath);

            Console.WriteLine($@"协议 {Scheme}:// 注册成功 ({exePath})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"协议 {Scheme}:// 注册失败: {ex.Message}");
        }
#endif
    }

    /// <summary>
    ///     移除协议注册（仅 Windows）
    /// </summary>
    public static void Unregister()
    {
#if WINDOWS
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Scheme}", false);
            Console.WriteLine($@"协议 {Scheme}:// 注册已移除");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"卸载协议失败: {ex.Message}");
        }
#endif
    }

    private static string? ExtractRemainderAfterScheme(string text)
    {
        var prefix = Scheme + "://";
        if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return text[prefix.Length..];

        if (text.StartsWith(Scheme + ":", StringComparison.OrdinalIgnoreCase))
            return text[(Scheme.Length + 1)..];

        return null;
    }

    private static IReadOnlyDictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
            return result;

        foreach (var pair in query.Split('&'))
        {
            if (string.IsNullOrEmpty(pair))
                continue;

            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            result[key] = value;
        }

        return result;
    }

    private static bool HasSpecialMode(IReadOnlyList<string> args)
    {
        var specialModes = new[]
        {
            "-update", "-updatev2", "--update-launcher", "--update-replace",
            "-runas", "-jump", "-open"
        };

        return args.Any(arg => specialModes.Contains(arg, StringComparer.OrdinalIgnoreCase));
    }

    private static string? ExtractProtocolPayload(IReadOnlyList<string> args)
    {
        for (var i = 0; i + 1 < args.Count; i++)
            if (args[i] == CliSwitch || args[i] == LegacyCliSwitch)
                return args[i + 1];

        return null;
    }

    private static bool TryAcquireFirstInstance()
    {
        try
        {
            var mutexName = OperatingSystem.IsWindows() ? MutexNameWindows : MutexNameUnix;
            _instanceMutex = new Mutex(true, mutexName, out var createdNew);

            if (createdNew)
                return true;

            try
            {
                return _instanceMutex.WaitOne(TimeSpan.Zero);
            }
            catch (AbandonedMutexException)
            {
                // 上一实例异常退出，未正确释放互斥体，本进程接管
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"创建单实例互斥体失败，继续启动: {ex.Message}");
            return true;
        }
    }

    private static async Task ListenAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($@"BedrockBoot 协议 IPC 服务已启动 (pipe: {PipeName})");

        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream server;
            try
            {
                server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"创建协议 IPC 管道失败: {ex.Message}");
                return;
            }

            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                break;
            }
            catch (IOException ex)
            {
                await server.DisposeAsync().ConfigureAwait(false);
                Console.WriteLine($@"等待协议连接失败: {ex.Message}");
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var connection = server;
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleClientAsync(connection).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"处理协议请求失败: {ex.Message}");
                }
            }, cancellationToken);
        }
    }

    private static async Task HandleClientAsync(NamedPipeServerStream server)
    {
        using (server)
        {
            var payload = await ReadLineBytesAsync(server, IpcReadTimeout).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine($@"收到 BedrockBoot 协议请求: {payload}");

                var request = Parse(payload);
                if (request == null)
                    Console.WriteLine($@"无法解析协议 URL: {payload}");
                else
                    Dispatch(request);
            }

            var ack = Encoding.UTF8.GetBytes("ok\n");
            await server.WriteAsync(ack, IpcShutdown.Token).ConfigureAwait(false);
            await server.FlushAsync(IpcShutdown.Token).ConfigureAwait(false);
        }
    }

    private static async Task<bool> TryForwardAsync(string payload)
    {
        var deadline = DateTime.UtcNow + ForwardDeadline;

        while (true)
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                using (var connectCts = new CancellationTokenSource(ConnectAttemptTimeout))
                {
                    await client.ConnectAsync(connectCts.Token).ConfigureAwait(false);
                }

                var data = Encoding.UTF8.GetBytes(payload + "\n");
                await client.WriteAsync(data, CancellationToken.None).ConfigureAwait(false);
                await client.FlushAsync(CancellationToken.None).ConfigureAwait(false);

                var ack = await ReadLineBytesAsync(client, ForwardDeadline).ConfigureAwait(false);
                return ack == "ok";
            }
            catch (OperationCanceledException) when (DateTime.UtcNow < deadline)
            {
                // 主实例可能仍在启动中，稍后重试
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
            }

            if (DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(150).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     从管道读取一行文本（以 \n 结尾）。
    ///     注意：不能在读取之前向同一管道创建 StreamWriter，否则会造成读取死锁，
    ///     因此这里统一使用底层字节读写，规避该问题。
    /// </summary>
    private static async Task<string?> ReadLineBytesAsync(PipeStream stream, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var buffer = new byte[8192];
        var text = new StringBuilder();

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token).ConfigureAwait(false);
            if (read <= 0)
                break;

            text.Append(Encoding.UTF8.GetString(buffer, 0, read));

            var current = text.ToString();
            var newlineIndex = current.IndexOf('\n');
            if (newlineIndex >= 0)
                return current[..newlineIndex].TrimEnd('\r');

            if (text.Length > MaxPayloadLength)
                return null;
        }

        return text.Length > 0 ? text.ToString().TrimEnd('\r') : null;
    }
}

/// <summary>
///     解析后的协议请求：路由 + 路径段 + 查询参数
/// </summary>
public sealed class ProtocolRequest
{
    public required string Raw { get; init; }
    public required string Route { get; init; }
    public required string[] Segments { get; init; }
    public required IReadOnlyDictionary<string, string> Query { get; init; }

    /// <summary>
    ///     获取查询参数值
    /// </summary>
    public string GetQuery(string key, string? defaultValue = null)
    {
        return Query.TryGetValue(key, out var value) ? value : (defaultValue ?? string.Empty);
    }
}