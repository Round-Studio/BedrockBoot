using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Pack.WebServer;

namespace BedrockBoot.Service.WebServer;

public class WebServer
{
    private readonly string[] _prefixes;
    private readonly HttpListener _listener;
    private readonly Dictionary<string, Action<HttpContext>> _routes = new();
    private bool _isRunning;
    
    private static WebServer? _instance;

    public static void StopInstance()
    {
        _instance?.Stop();
    }

    public WebServer(params string[] prefixes)
    {
        if (!HttpListener.IsSupported)
            throw new NotSupportedException("需要 Windows XP SP2 或 Server 2003 以上系统。");
        _prefixes = prefixes;

        _listener = new HttpListener();
        foreach (var prefix in prefixes)
        {
            _listener.Prefixes.Add(prefix);
        }
        _instance = this;
    }

    /// <summary>
    /// 注册路由事件
    /// </summary>
    /// <param name="method">GET, POST, etc.</param>
    /// <param name="path">以 / 开头，例如 /login</param>
    /// <param name="handler">处理逻辑</param>
    public void RegisterRoute(string method, string path, Action<HttpContext> handler)
    {
        string key = $"{method.ToUpper()}:{path.ToLower()}";
        _routes[key] = handler;
    }

    public void Start()
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine(@$"Started on {string.Join(" ", _prefixes)}");

        Task.Run(async () =>
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"处理请求时出错: {ex.Message}");
                }
            }
        });
    }

    private void ProcessRequest(HttpListenerContext listenerContext)
    {
        var request = listenerContext.Request;
        var response = listenerContext.Response;
        
        // 匹配路由 (忽略 URL 中的查询参数部分)
        string path = request.Url?.AbsolutePath.ToLower() ?? "/";
        string key = $"{request.HttpMethod}:{path}";

        var httpContext = new HttpContext(request, response);

        if (_routes.TryGetValue(key, out var handler))
        {
            handler(httpContext);
        }
        else
        {
            httpContext.SendResponse("404 Not Found", "text/plain", HttpStatusCode.NotFound);
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
    }
}