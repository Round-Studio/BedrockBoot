using System.Net;
using System.Text;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

// 1. 定义 WebSocket 服务端行为
public class MinecraftCommandService : WebSocketBehavior
{
    // 静态引用，用于从外部访问当前活动的服务实例
    public static MinecraftCommandService? Instance { get; private set; }

    protected override void OnOpen()
    {
        // 保存当前实例供外部调用
        Instance = this;
        Console.WriteLine("游戏客户端已连接！");
        
        // 可选：订阅聊天事件
        // SendSubscribe("PlayerMessage");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        // 断开连接时清空引用，避免发送命令到无效连接
        if (Instance == this)
        {
            Instance = null;
        }
        Console.WriteLine($"游戏客户端已断开: {e.Reason}");
    }

    // 处理游戏发来的消息（用于监听事件）
    protected override void OnMessage(MessageEventArgs e)
    {
        Console.WriteLine($"收到游戏消息: {e.Data}");
        // 可以在这里解析游戏事件
    }

    // 发送命令到游戏的核心方法
    public void SendCommand(string command)
    {
        if (!IsAlive)
        {
            Console.WriteLine("WebSocket未连接，无法发送命令。");
            return;
        }

        var payload = new
        {
            header = new
            {
                version = 1,
                requestId = Guid.NewGuid().ToString(),
                messagePurpose = "commandRequest",
                messageType = "commandRequest"
            },
            body = new
            {
                commandLine = command,
                origin = new { type = "player" }
            }
        };

        string json = JsonSerializer.Serialize(payload);
        Send(json);
        Console.WriteLine($"已发送命令: {command}");
    }

    // 订阅事件（可选）
    private void SendSubscribe(string eventName)
    {
        var payload = new
        {
            header = new
            {
                version = 1,
                requestId = Guid.NewGuid().ToString(),
                messagePurpose = "subscribe",
                messageType = "commandRequest"
            },
            body = new { eventName = eventName }
        };
        Send(JsonSerializer.Serialize(payload));
    }
}

// 2. 主程序
class Program
{
    private static WebSocketServer? _wssv;

    static void Main(string[] args)
    {
        // 启动 WebSocket 服务
        _wssv = new WebSocketServer("ws://localhost:8080");
        _wssv.AddWebSocketService<MinecraftCommandService>("/mc");
        _wssv.Start();
        Console.WriteLine("WebSocket 服务已启动，地址: ws://localhost:8080/mc");
        Console.WriteLine("请在游戏内输入: /connect localhost:8080/mc");

        // 启动 HTTP 服务器用于接收外部命令
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:8081/");
        httpListener.Start();
        Console.WriteLine("HTTP 命令接口已启动，地址: http://localhost:8081/command");

        // 异步处理 HTTP 请求
        Task.Run(async () => await HandleHttpRequests(httpListener));

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
        _wssv.Stop();
        httpListener.Stop();
    }

    private static async Task HandleHttpRequests(HttpListener listener)
    {
        while (listener.IsListening)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/command")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                try
                {
                    var json = JsonDocument.Parse(body);
                    string command = json.RootElement.GetProperty("command").GetString() ?? "";

                    // ===== 修正后的调用方式 =====
                    // 直接使用静态 Instance 获取活动的服务实例
                    var serviceInstance = MinecraftCommandService.Instance;
                    if (serviceInstance != null)
                    {
                        serviceInstance.SendCommand(command);
                        await SendResponse(response, 200, "{\"status\":\"ok\"}");
                    }
                    else
                    {
                        await SendResponse(response, 503, "{\"status\":\"error\",\"message\":\"未连接到游戏\"}");
                    }
                }
                catch (Exception ex)
                {
                    await SendResponse(response, 400, $"{{\"status\":\"error\",\"message\":\"{ex.Message}\"}}");
                }
            }
            else
            {
                await SendResponse(response, 404, "{\"status\":\"not found\"}");
            }
            response.Close();
        }
    }

    private static async Task SendResponse(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        await Task.CompletedTask;
    }
}