using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockBoot.Service.Protocol;

/// <summary>
///     HTTP 服务器，支持路由注册和参数解析
/// </summary>
public class ProtocolService : IDisposable
{
    /// <summary>
    ///     路由处理委托
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="parameters">查询参数和路由参数</param>
    /// <returns>返回 Task</returns>
    public delegate Task RouteHandler(HttpListenerContext context, RouteParameters parameters);

    // 中间件列表
    private readonly List<Func<HttpListenerContext, Func<Task>, Task>> _middlewares = new();

    // 路由字典：路由路径 -> 处理函数
    private readonly ConcurrentDictionary<string, RouteHandler> _routes = new();
    private CancellationTokenSource _cancellationTokenSource;

    // 默认路由处理
    private RouteHandler _defaultRouteHandler;
    private bool _isRunning;

    private HttpListener _listener;

    /// <summary>
    ///     构造函数
    /// </summary>
    public ProtocolService()
    {
        // 设置默认路由处理（404）
        _defaultRouteHandler = async (context, parameters) =>
        {
            await WriteResponseAsync(context, 404, new
            {
                error = "Not Found",
                message = $"Route '{parameters.Url.AbsolutePath}' not found",
                timestamp = DateTime.UtcNow
            });
        };
    }

    public int ServicePort { get; set; } = 43956;
    public string ServiceHost { get; set; } = "127.0.0.1";

    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationTokenSource?.Dispose();
        _listener?.Close();
    }

    /// <summary>
    ///     启动 HTTP 服务器
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning)
            throw new InvalidOperationException("Server is already running");

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{ServiceHost}:{ServicePort}/");

            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;

            _listener.Start();
            Console.WriteLine($@"HTTP Server started on http://{ServiceHost}:{ServicePort}/");

            // 开始监听请求
            await StartListeningAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _isRunning = false;
            Console.WriteLine($@"Failed to start server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     停止 HTTP 服务器
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _cancellationTokenSource?.Cancel();
        _isRunning = false;

        if (_listener != null && _listener.IsListening)
        {
            _listener.Stop();
            _listener.Close();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     注册 GET 路由
    /// </summary>
    /// <param name="path">路由路径，支持占位符如 /api/users/{id}</param>
    /// <param name="handler">处理函数</param>
    public void Get(string path, RouteHandler handler)
    {
        RegisterRoute("GET", path, handler);
    }

    /// <summary>
    ///     注册 POST 路由
    /// </summary>
    public void Post(string path, RouteHandler handler)
    {
        RegisterRoute("POST", path, handler);
    }

    /// <summary>
    ///     注册 PUT 路由
    /// </summary>
    public void Put(string path, RouteHandler handler)
    {
        RegisterRoute("PUT", path, handler);
    }

    /// <summary>
    ///     注册 DELETE 路由
    /// </summary>
    public void Delete(string path, RouteHandler handler)
    {
        RegisterRoute("DELETE", path, handler);
    }

    /// <summary>
    ///     注册任意 HTTP 方法的路由
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="path">路由路径</param>
    /// <param name="handler">处理函数</param>
    public void RegisterRoute(string method, string path, RouteHandler handler)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        // 标准化路径
        path = NormalizePath(path);

        // 创建路由键：方法 + 路径
        var routeKey = $"{method.ToUpper()}:{path}";

        // 添加或更新路由
        _routes[routeKey] = handler;

        Console.WriteLine($@"Registered route: {method} {path}");
    }

    /// <summary>
    ///     注册中间件
    /// </summary>
    public void Use(Func<HttpListenerContext, Func<Task>, Task> middleware)
    {
        _middlewares.Add(middleware);
    }

    /// <summary>
    ///     设置默认路由处理（404 处理）
    /// </summary>
    public void SetDefaultHandler(RouteHandler handler)
    {
        _defaultRouteHandler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    ///     开始监听请求
    /// </summary>
    private async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                // 异步获取上下文
                var context = await _listener.GetContextAsync();

                // 处理请求（不阻塞监听循环）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessRequestAsync(context);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"Error processing request: {ex.Message}");
                        await WriteResponseAsync(context, 500, new
                        {
                            error = "Internal Server Error",
                            message = ex.Message,
                            timestamp = DateTime.UtcNow
                        });
                    }
                }, cancellationToken);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // 监听器被停止
                break;
            }
            catch (OperationCanceledException)
            {
                // 操作被取消
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error in listener: {ex.Message}");
                await Task.Delay(1000); // 避免快速重试
            }
    }

    /// <summary>
    ///     处理 HTTP 请求
    /// </summary>
    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        // 执行中间件
        await ExecuteMiddlewares(context);

        var request = context.Request;
        var response = context.Response;

        // 读取请求体（如果有）
        string body = null;
        if (request.HasEntityBody)
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

        // 解析查询参数
        var queryParams = ParseQueryString(request.Url.Query);

        // 尝试匹配路由
        var (routeHandler, routeParams) = FindRouteHandler(request.HttpMethod, request.Url.AbsolutePath);

        // 创建路由参数对象
        var parameters = new RouteParameters(
            queryParams,
            routeParams,
            body,
            request.Url,
            request.HttpMethod
        );

        // 执行路由处理函数
        if (routeHandler != null)
            await routeHandler(context, parameters);
        else
            // 使用默认处理
            await _defaultRouteHandler(context, parameters);
    }

    /// <summary>
    ///     执行中间件链
    /// </summary>
    private async Task ExecuteMiddlewares(HttpListenerContext context)
    {
        if (_middlewares.Count == 0)
            return;

        var index = 0;
        Func<Task> next = null;
        next = async () =>
        {
            if (index < _middlewares.Count)
            {
                var middleware = _middlewares[index++];
                await middleware(context, next);
            }
        };

        await next();
    }

    /// <summary>
    ///     查找匹配的路由处理函数
    /// </summary>
    private (RouteHandler handler, IReadOnlyDictionary<string, string> routeParams)
        FindRouteHandler(string method, string requestPath)
    {
        // 标准化请求路径
        requestPath = NormalizePath(requestPath);

        // 精确匹配
        var exactKey = $"{method.ToUpper()}:{requestPath}";
        if (_routes.TryGetValue(exactKey, out var exactHandler))
            return (exactHandler, new Dictionary<string, string>());

        // 通配符/参数匹配
        foreach (var kvp in _routes)
        {
            var routeKey = kvp.Key;
            var routeMethod = routeKey.Split(':')[0];

            // 检查方法是否匹配
            if (method.ToUpper() != routeMethod)
                continue;

            var routePath = routeKey.Substring(routeMethod.Length + 1);

            // 尝试匹配带参数的路由
            if (TryMatchRouteWithParams(routePath, requestPath, out var routeParams)) return (kvp.Value, routeParams);
        }

        return (null, null);
    }

    /// <summary>
    ///     尝试匹配带参数的路由
    /// </summary>
    private bool TryMatchRouteWithParams(string routePath, string requestPath,
        out IReadOnlyDictionary<string, string> routeParams)
    {
        routeParams = null;

        var routeSegments = routePath.Split('/');
        var requestSegments = requestPath.Split('/');

        if (routeSegments.Length != requestSegments.Length)
            return false;

        var paramsDict = new Dictionary<string, string>();

        for (var i = 0; i < routeSegments.Length; i++)
        {
            var routeSeg = routeSegments[i];
            var requestSeg = requestSegments[i];

            if (routeSeg.StartsWith("{") && routeSeg.EndsWith("}"))
            {
                // 参数段
                var paramName = routeSeg.Trim('{', '}');
                paramsDict[paramName] = requestSeg;
            }
            else if (routeSeg != requestSeg)
            {
                // 静态段不匹配
                return false;
            }
        }

        routeParams = paramsDict;
        return true;
    }

    /// <summary>
    ///     解析查询字符串
    /// </summary>
    private IReadOnlyDictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(query) || query == "?")
            return result;

        // 去掉开头的 ?
        if (query.StartsWith("?"))
            query = query.Substring(1);

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            else if (parts.Length == 1)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                result[key] = string.Empty;
            }
        }

        return result;
    }

    /// <summary>
    ///     标准化路径
    /// </summary>
    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        // 确保以 / 开头
        if (!path.StartsWith("/"))
            path = "/" + path;

        // 去掉末尾的 /
        if (path.EndsWith("/") && path.Length > 1)
            path = path.TrimEnd('/');

        return path;
    }

    /// <summary>
    ///     写入 JSON 响应
    /// </summary>
    public static async Task WriteResponseAsync(HttpListenerContext context, int statusCode, object data)
    {
        var response = context.Response;
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    /// <summary>
    ///     写入文本响应
    /// </summary>
    public static async Task WriteTextResponseAsync(HttpListenerContext context, int statusCode, string text,
        string contentType = "text/plain")
    {
        var response = context.Response;
        response.StatusCode = statusCode;
        response.ContentType = contentType;

        var buffer = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    /// <summary>
    ///     写入错误响应
    /// </summary>
    public static async Task WriteErrorResponseAsync(HttpListenerContext context, int statusCode, string error,
        string message = null)
    {
        await WriteResponseAsync(context, statusCode, new
        {
            error,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    ///     重定向
    /// </summary>
    public static void Redirect(HttpListenerContext context, string url, bool permanent = false)
    {
        var response = context.Response;
        response.StatusCode = permanent ? 301 : 302;
        response.RedirectLocation = url;
        response.Close();
    }

    /// <summary>
    ///     获取所有已注册的路由
    /// </summary>
    public IEnumerable<string> GetRegisteredRoutes()
    {
        return _routes.Keys.Select(key =>
        {
            var parts = key.Split(':');
            return $"{parts[0]} {parts[1]}";
        }).OrderBy(r => r);
    }

    /// <summary>
    ///     路由参数
    /// </summary>
    public class RouteParameters
    {
        public RouteParameters(
            IReadOnlyDictionary<string, string> query,
            IReadOnlyDictionary<string, string> route,
            string body,
            Uri url,
            string method)
        {
            Query = query;
            Route = route;
            Body = body;
            Url = url;
            Method = method;
        }

        /// <summary>
        ///     查询参数（URL 中的 ?key=value）
        /// </summary>
        public IReadOnlyDictionary<string, string> Query { get; }

        /// <summary>
        ///     路由参数（路径中的占位符，如 /users/{id}）
        /// </summary>
        public IReadOnlyDictionary<string, string> Route { get; }

        /// <summary>
        ///     请求体（如果有）
        /// </summary>
        public string Body { get; }

        /// <summary>
        ///     完整的 URL
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        ///     HTTP 方法
        /// </summary>
        public string Method { get; }

        /// <summary>
        ///     获取查询参数值
        /// </summary>
        public string GetQuery(string key, string defaultValue = null)
        {
            return Query.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     获取路由参数值
        /// </summary>
        public string GetRoute(string key, string defaultValue = null)
        {
            return Route.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        ///     尝试获取查询参数值
        /// </summary>
        public bool TryGetQuery(string key, out string value)
        {
            return Query.TryGetValue(key, out value);
        }

        /// <summary>
        ///     尝试获取路由参数值
        /// </summary>
        public bool TryGetRoute(string key, out string value)
        {
            return Route.TryGetValue(key, out value);
        }

        /// <summary>
        ///     解析请求体为 JSON 对象
        /// </summary>
        public T GetBodyAsJson<T>(JsonSerializerOptions options = null)
        {
            if (string.IsNullOrEmpty(Body))
                return default;

            return JsonSerializer.Deserialize<T>(Body, options);
        }
    }
}