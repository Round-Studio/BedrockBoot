/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Standard.Entity.Game.Pack.Server;

namespace BedrockBoot.Models.Pack.Game.Server;

public class ServerChecker
{
    private const int DEFAULT_PORT = 19132;
    private const int TIMEOUT_MS = 3000;

    // 传输协议标识
    private const string TRANSPORT_RAKNET = "RakNet";
    private const string TRANSPORT_NETHERNET = "NetherNet";

    // NetherNet 状态查询端点：基岩客户端在建立 NetherNet 连接前会先请求该端点获取服务器信息
    private const string NETHERNET_JOIN_PATH = "/v1/join";
    private const string NETHERNET_USER_AGENT = "libhttpclient/1.0.0.0";

    /// <summary>
    ///     查询服务器状态：RakNet 与 NetherNet 并存，并行探测，任一协议先返回在线即采用
    /// </summary>
    public async Task<ServerStatusResponse> GetServerStatusAsync(ServerItemInfo info)
    {
        var rakNetTask = PingViaRakNetAsync(info);
        var netherNetTask = PingViaNetherNetAsync(info);

        while (true)
        {
            var finished = await Task.WhenAny(rakNetTask, netherNetTask);

            if ("online".Equals(finished.Result.Status, StringComparison.OrdinalIgnoreCase))
                return finished.Result;

            // 两个协议均失败：沿用 RakNet 的失败信息展示
            if (rakNetTask.IsCompleted && netherNetTask.IsCompleted)
                return rakNetTask.Result;
        }
    }

    // RakNet Magic值
    private static readonly byte[] RAKNET_MAGIC = new byte[]
    {
        0x00, 0xFF, 0xFF, 0x00,
        0xFE, 0xFE, 0xFE, 0xFE,
        0xFD, 0xFD, 0xFD, 0xFD,
        0x12, 0x34, 0x56, 0x78
    };

    /// <summary>
    ///     使用RakNet协议查询服务器，返回minebbs格式的数据
    /// </summary>
    public async Task<ServerStatusResponse> PingViaRakNetAsync(ServerItemInfo info)
    {
        var ip = info.ServerAddress;
        var portNum = info.ServerPort;

        var stopwatch = Stopwatch.StartNew();
        var response = new ServerStatusResponse
        {
            Host = $"{ip}:{portNum}",
            Type = "Bedrock"
        };

        try
        {
            // 使用RakNet协议查询
            using (var udpClient = new UdpClient())
            {
                udpClient.Client.ReceiveTimeout = TIMEOUT_MS;
                udpClient.Client.SendTimeout = TIMEOUT_MS;

                // 解析IP地址
                IPAddress[] addresses;
                try
                {
                    addresses = await Dns.GetHostAddressesAsync(ip);
                }
                catch
                {
                    // 如果DNS解析失败，尝试直接解析为IP地址
                    if (!IPAddress.TryParse(ip, out var parsedAddress))
                    {
                        response.Status = "offline";
                        response.PureMOTD = string.Format(I18nManager.Instance["Server.Error.ResolveHost"], ip);
                        return response;
                    }

                    addresses = new[] { parsedAddress };
                }

                if (addresses.Length == 0)
                {
                    response.Status = "offline";
                    response.PureMOTD = string.Format(I18nManager.Instance["Server.Error.ResolveHost"], ip);
                    return response;
                }

                var serverEndpoint = new IPEndPoint(addresses[0], portNum);

                // 构建RakNet Ping数据包
                var pingPacket = BuildRakNetPing();

                // 发送请求
                await udpClient.SendAsync(pingPacket, pingPacket.Length, serverEndpoint);

                // 接收响应
                var receiveResult = await udpClient.ReceiveAsync();

                // 计算延迟
                var latency = stopwatch.ElapsedMilliseconds;

                // 解析RakNet响应
                if (TryParseRakNetPong(receiveResult.Buffer, out var motdString, out var version, out var online,
                        out var max, out var gameMode))
                {
                    // 构建minebbs格式的响应
                    response.Status = "online";
                    response.MOTD = motdString;
                    response.PureMOTD = StripFormatCodes(motdString);
                    response.Version = version;
                    response.Players = new ServerStatusResponse.PlayersData
                    {
                        Online = online.ToString(),
                        Max = max.ToString()
                    };
                    response.Gamemode = gameMode;
                    response.Delay = (int)latency;
                    response.Protocol = version;
                    response.LevelName = "Unknown"; // RakNet协议中可能没有这个字段
                    response.Cached = false;
                    response.Transport = TRANSPORT_RAKNET;
                }
                else
                {
                    response.Status = "offline";
                    response.PureMOTD = I18nManager.Instance["Server.Error.ParseResponse"];
                }
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            response.Status = "offline";
            response.PureMOTD = I18nManager.Instance["Server.Error.Timeout"];
        }
        catch (Exception ex)
        {
            response.Status = "offline";
            response.PureMOTD = string.Format(I18nManager.Instance["Server.Error.QueryFailed"], ex.Message);
        }

        return response;
    }

    /// <summary>
    ///     使用NetherNet协议查询服务器：请求 HTTP(S) 的 /v1/join 端点，返回minebbs格式的数据
    /// </summary>
    private async Task<ServerStatusResponse> PingViaNetherNetAsync(ServerItemInfo info)
    {
        var ip = info.ServerAddress;
        var portNum = info.ServerPort;

        var response = new ServerStatusResponse
        {
            Host = $"{ip}:{portNum}",
            Type = "Bedrock"
        };

        try
        {
            var (body, delayMs) = await QueryNetherNetJoinAsync(ip, portNum);

            if (!TryParseNetherNetJoin(body, out var motd, out var version, out var level, out var players,
                    out var maxPlayers, out var gameType, out var protocol))
            {
                response.Status = "offline";
                response.PureMOTD = I18nManager.Instance["Server.Error.ParseResponse"];
                return response;
            }

            response.Status = "online";
            response.MOTD = motd;
            response.PureMOTD = StripFormatCodes(motd);
            response.Version = string.IsNullOrEmpty(version) ? "Unknown" : version;
            response.Players = new ServerStatusResponse.PlayersData
            {
                Online = players.ToString(),
                Max = maxPlayers.ToString()
            };
            response.Gamemode = MapGameType(gameType);
            response.Delay = (int)delayMs;
            response.Protocol = protocol >= 0 ? protocol.ToString() : version;
            response.LevelName = string.IsNullOrEmpty(level) ? "Unknown" : level;
            response.Cached = false;
            response.Transport = TRANSPORT_NETHERNET;
        }
        catch (OperationCanceledException)
        {
            response.Status = "offline";
            response.PureMOTD = I18nManager.Instance["Server.Error.Timeout"];
        }
        catch (Exception ex)
        {
            response.Status = "offline";
            response.PureMOTD = string.Format(I18nManager.Instance["Server.Error.QueryFailed"], ex.Message);
        }

        return response;
    }

    /// <summary>
    ///     请求 NetherNet 的 /v1/join 端点：先 HTTPS，TLS/连接层失败退回 HTTP
    /// </summary>
    private async Task<(string Body, long DelayMs)> QueryNetherNetJoinAsync(string host, int port)
    {
        try
        {
            return await SendNetherNetJoinAsync(host, port, "https");
        }
        catch (HttpRequestException)
        {
            // HTTPS 握手或连接失败（证书/协议/EOF），按基岩客户端行为退回 HTTP 端点
            return await SendNetherNetJoinAsync(host, port, "http");
        }
    }

    // NetherNet 查询共用单例 HttpClient：每条服务器各建 handler 会在调用线程上触发
    // 系统/首次请求的同步初始化（WinHTTP 代理解析等），在 UI 线程上会造成卡死
    private static readonly HttpClient NetherNetHttpClient = CreateNetherNetClient();

    private static HttpClient CreateNetherNetClient()
    {
        var handler = new SocketsHttpHandler
        {
            // NetherNet 端点普遍使用自签名证书；此处仅为状态查询且不发送任何凭据，故不校验证书链
            SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true },
            AllowAutoRedirect = false,
            // 与 RakNet UDP 一致走直连：绕开系统代理的同步解析路径
            UseProxy = false,
            PooledConnectionLifetime = TimeSpan.FromSeconds(30),
            ConnectTimeout = TimeSpan.FromMilliseconds(TIMEOUT_MS)
        };

        return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    private static async Task<(string Body, long DelayMs)> SendNetherNetJoinAsync(string host, int port, string scheme)
    {
        // IPv6 字面量需要方括号才能构成合法 URI
        var uriHost = Uri.CheckHostName(host) == UriHostNameType.IPv6 ? $"[{host}]" : host;

        var stopwatch = Stopwatch.StartNew();
        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TIMEOUT_MS)))
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get,
                       $"{scheme}://{uriHost}:{port}{NETHERNET_JOIN_PATH}"))
            {
                request.Headers.UserAgent.ParseAdd(NETHERNET_USER_AGENT);
                request.Headers.Connection.Add("Keep-Alive");

                using (var response =
                           await NetherNetHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                               timeoutCts.Token))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new HttpRequestException(
                            $"Unexpected response code: {(int)response.StatusCode}");

                    var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                    stopwatch.Stop();
                    return (body, stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }

    /// <summary>
    ///     解析 NetherNet /v1/join 的 JSON 响应
    /// </summary>
    private static bool TryParseNetherNetJoin(string body, out string motd, out string version, out string level,
        out int players, out int maxPlayers, out int gameType, out int protocol)
    {
        motd = "";
        version = "";
        level = "";
        players = 0;
        maxPlayers = 0;
        gameType = -1;
        protocol = -1;

        try
        {
            using (var doc = JsonDocument.Parse(body))
            {
                var root = doc.RootElement;

                // 至少要有 name 字段才算有效的 NetherNet 应答，避免把端点上的其他 HTTP 服务误判为在线
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("name", out var nameEl) ||
                    nameEl.ValueKind != JsonValueKind.String)
                    return false;

                motd = nameEl.GetString() ?? "";
                version = GetNetherNetString(root, "version") ?? "";
                level = GetNetherNetString(root, "level") ?? "";
                players = GetNetherNetInt(root, "players", 0);
                maxPlayers = GetNetherNetInt(root, "maxPlayers", 0);
                gameType = GetNetherNetInt(root, "gameType", -1);
                protocol = GetNetherNetInt(root, "protocol", -1);
                return true;
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    private static string GetNetherNetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    private static int GetNetherNetInt(JsonElement root, string name, int fallback)
    {
        return root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number &&
               el.TryGetInt32(out var value)
            ? value
            : fallback;
    }

    /// <summary>
    ///     将 NetherNet 响应中的 gameType 数值映射为游戏模式名称
    /// </summary>
    private static string MapGameType(int gameType)
    {
        return gameType switch
        {
            0 => "Survival",
            1 => "Creative",
            2 => "Adventure",
            3 => "Spectator",
            _ => "Unknown"
        };
    }

    /// <summary>
    ///     构建RakNet未连接Ping数据包
    /// </summary>
    private byte[] BuildRakNetPing()
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            // 数据包ID: 0x01 (Unconnected Ping)
            writer.Write((byte)0x01);

            // 客户端时间戳
            writer.Write(Environment.TickCount64);

            // Magic值
            writer.Write(RAKNET_MAGIC);

            // 客户端GUID
            writer.Write((long)new Random().Next());

            return stream.ToArray();
        }
    }

    /// <summary>
    ///     解析RakNet未连接Pong响应
    /// </summary>
    private bool TryParseRakNetPong(byte[] data, out string motd, out string version, out int online, out int max,
        out string gameMode)
    {
        motd = "";
        version = "";
        online = 0;
        max = 0;
        gameMode = "";

        try
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                // 验证数据包ID: 0x1c (Unconnected Pong)
                var packetId = reader.ReadByte();
                if (packetId != 0x1c)
                    return false;

                // 跳过客户端时间戳
                reader.ReadInt64();

                // 跳过服务器GUID
                reader.ReadInt64();

                // 跳过Magic值
                reader.ReadBytes(16);

                // 读取字符串长度
                var length = reader.ReadUInt16();

                // 读取MOTD字符串
                var motdBytes = reader.ReadBytes(length);
                var motdString = Encoding.UTF8.GetString(motdBytes);

                // 解析MOTD字符串格式: MCPE;ServerName;Protocol;Version;Online;Max;GUID;World;GameMode;...
                var parts = motdString.Split(';');
                if (parts.Length >= 9)
                {
                    motd = parts[1]; // 服务器名称
                    version = parts[3]; // 游戏版本

                    if (int.TryParse(parts[4], out online))
                        online = online;

                    if (int.TryParse(parts[5], out max))
                        max = max;

                    gameMode = parts[8]; // 游戏模式

                    return true;
                }
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    /// <summary>
    ///     移除Minecraft格式代码
    /// </summary>
    private string StripFormatCodes(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 移除§符号及其后的一个字符（颜色/格式代码）
        var sb = new StringBuilder();
        var skipNext = false;

        foreach (var c in text)
        {
            if (c == '§')
            {
                skipNext = true;
                continue;
            }

            if (skipNext)
            {
                skipNext = false;
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}