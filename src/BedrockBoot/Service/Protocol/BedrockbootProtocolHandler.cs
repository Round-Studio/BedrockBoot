using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BedrockBoot.Service.Protocol;

public static class BedrockbootProtocolHandler
{
    private const string IpcBaseUrl = "http://127.0.0.1:43956";

    private static readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    { Timeout = TimeSpan.FromSeconds(3) };

    public static string? PendingCommand { get; set; }

    public static bool TrySendCommand(string command)
    {
        return TrySendToRunningInstance(command);
    }

    public static bool HandleProtocolUrl(string url)
    {
        var command = ParseProtocolUrl(url);
        if (string.IsNullOrEmpty(command))
            return false;

        Console.WriteLine($@"BedrockBoot 协议: {url} -> {command}");

        if (TrySendToRunningInstance(command))
        {
            Console.WriteLine(@"已转发至运行中的主窗口进程");
            return true;
        }

        Console.WriteLine(@"未检测到运行中的主窗口，暂存命令等待启动后执行");
        PendingCommand = command;
        return false;
    }

    public static string? ParseProtocolUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var lower = url.Trim();

        if (lower.StartsWith("bedrockboot://", StringComparison.OrdinalIgnoreCase))
            return lower["bedrockboot://".Length..];

        if (lower.StartsWith("bedrockboot:", StringComparison.OrdinalIgnoreCase))
            return lower["bedrockboot:".Length..];

        return null;
    }

    private static bool TrySendToRunningInstance(string command)
    {
        try
        {
            var response = _httpClient
                .GetAsync($"{IpcBaseUrl}/shell?command={Uri.EscapeDataString("bedrockboot://" + command)}")
                .Result;

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task ExecutePendingCommandAsync()
    {
        if (string.IsNullOrEmpty(PendingCommand))
            return;

        var command = PendingCommand;
        PendingCommand = null;

        Console.WriteLine($@"执行暂存的协议命令: {command}");
        await ExecuteCommandAsync(command);
    }

    public static async Task ExecuteCommandAsync(string command)
    {
        if (string.IsNullOrEmpty(command))
            return;

        var segments = command.Split('?', 2);
        var pathPart = segments[0].Trim('/');
        var queryPart = segments.Length > 1 ? segments[1] : "";

        var pathSegments = pathPart.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 0)
        {
            Console.WriteLine(@"协议命令为空");
            return;
        }

        var routeName = pathSegments[0];
        var route = ProtocolRouteRegistry.Instance.Get(routeName);
        if (route == null)
        {
            Console.WriteLine($@"未注册的路由: bedrockboot://{routeName}");
            Console.WriteLine($@"已注册的路由: {string.Join(", ", ProtocolRouteRegistry.Instance.GetRegisteredRouteNames())}");
            return;
        }

        var routeSegments = pathSegments.Skip(1).ToArray();
        var queryParams = ParseQueryString(queryPart);

        Console.WriteLine($@"执行路由: bedrockboot://{routeName}/{string.Join("/", routeSegments)}");
        await route.ExecuteAsync(routeSegments, queryParams);
    }

    private static IReadOnlyDictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
            return result;

        var pairs = query.Split('&');
        foreach (var pair in pairs)
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
}
