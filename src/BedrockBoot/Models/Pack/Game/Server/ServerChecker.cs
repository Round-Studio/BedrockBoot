using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.Game.Server;

public class ServerChecker
{
    private const int DEFAULT_PORT = 19132;
    private const int TIMEOUT_MS = 3000;
    
    // RakNet Magic值
    private static readonly byte[] RAKNET_MAGIC = new byte[]
    {
        0x00, 0xFF, 0xFF, 0x00,
        0xFE, 0xFE, 0xFE, 0xFE,
        0xFD, 0xFD, 0xFD, 0xFD,
        0x12, 0x34, 0x56, 0x78
    };
    
    /// <summary>
    /// 使用RakNet协议查询服务器，返回minebbs格式的数据
    /// </summary>
    public async Task<ServerStatusResponse> GetServerStatusAsync(ServerItemInfo info)
    {
        var ip = info.ServerAddress;
        var portNum = info.ServerPort;
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
                    if (!IPAddress.TryParse(ip, out IPAddress parsedAddress))
                    {
                        response.Status = "offline";
                        response.PureMOTD = $"无法解析主机名: {ip}";
                        return response;
                    }
                    addresses = new IPAddress[] { parsedAddress };
                }
                
                if (addresses.Length == 0)
                {
                    response.Status = "offline";
                    response.PureMOTD = $"无法解析主机名: {ip}";
                    return response;
                }
                
                var serverEndpoint = new IPEndPoint(addresses[0], portNum);
                
                // 构建RakNet Ping数据包
                byte[] pingPacket = BuildRakNetPing();
                
                // 发送请求
                await udpClient.SendAsync(pingPacket, pingPacket.Length, serverEndpoint);
                
                // 接收响应
                var receiveResult = await udpClient.ReceiveAsync();
                
                // 计算延迟
                long latency = stopwatch.ElapsedMilliseconds;
                
                // 解析RakNet响应
                if (TryParseRakNetPong(receiveResult.Buffer, out string motdString, out string version, out int online, out int max, out string gameMode))
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
                }
                else
                {
                    response.Status = "offline";
                    response.PureMOTD = "无法解析服务器响应";
                }
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            response.Status = "offline";
            response.PureMOTD = "请求超时";
        }
        catch (Exception ex)
        {
            response.Status = "offline";
            response.PureMOTD = $"查询失败: {ex.Message}";
        }
        
        return response;
    }
    
    /// <summary>
    /// 构建RakNet未连接Ping数据包
    /// </summary>
    private byte[] BuildRakNetPing()
    {
        using (var stream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(stream))
        {
            // 数据包ID: 0x01 (Unconnected Ping)
            writer.Write((byte)0x01);
            
            // 客户端时间戳
            writer.Write((long)Environment.TickCount64);
            
            // Magic值
            writer.Write(RAKNET_MAGIC);
            
            // 客户端GUID
            writer.Write((long)new Random().Next());
            
            return stream.ToArray();
        }
    }
    
    /// <summary>
    /// 解析RakNet未连接Pong响应
    /// </summary>
    private bool TryParseRakNetPong(byte[] data, out string motd, out string version, out int online, out int max, out string gameMode)
    {
        motd = "";
        version = "";
        online = 0;
        max = 0;
        gameMode = "";
        
        try
        {
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                // 验证数据包ID: 0x1c (Unconnected Pong)
                byte packetId = reader.ReadByte();
                if (packetId != 0x1c)
                    return false;
                
                // 跳过客户端时间戳
                reader.ReadInt64();
                
                // 跳过服务器GUID
                reader.ReadInt64();
                
                // 跳过Magic值
                reader.ReadBytes(16);
                
                // 读取字符串长度
                ushort length = reader.ReadUInt16();
                
                // 读取MOTD字符串
                byte[] motdBytes = reader.ReadBytes(length);
                string motdString = Encoding.UTF8.GetString(motdBytes);
                
                // 解析MOTD字符串格式: MCPE;ServerName;Protocol;Version;Online;Max;GUID;World;GameMode;...
                string[] parts = motdString.Split(';');
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
    /// 移除Minecraft格式代码
    /// </summary>
    private string StripFormatCodes(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        // 移除§符号及其后的一个字符（颜色/格式代码）
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool skipNext = false;
        
        foreach (char c in text)
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