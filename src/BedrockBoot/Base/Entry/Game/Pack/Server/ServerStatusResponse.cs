using System;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.Server;

// 主响应模型
public class ServerStatusResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("host")]
    public string Host { get; set; }
    
    [JsonPropertyName("motd")]
    public string MOTD { get; set; }
    
    [JsonPropertyName("pureMotd")]
    public string PureMOTD { get; set; }
    
    [JsonPropertyName("version")]
    public string Version { get; set; }
    
    [JsonPropertyName("players")]
    public PlayersData Players { get; set; }
    
    [JsonPropertyName("gamemode")]
    public string Gamemode { get; set; }
    
    [JsonPropertyName("delay")]
    public int Delay { get; set; }
    
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; }
    
    [JsonPropertyName("levelname")]
    public string LevelName { get; set; }
    
    [JsonPropertyName("cached")]
    public bool Cached { get; set; }
    
    public class PlayersData
    {
        [JsonPropertyName("online")]
        public string Online { get; set; }
    
        [JsonPropertyName("max")]
        public string Max { get; set; }
    
        // 辅助方法：获取在线玩家数（整数形式）
        public int GetOnlineCount()
        {
            if (int.TryParse(Online, out int result))
                return result;
            return 0;
        }
    
        // 辅助方法：获取最大玩家数（整数形式）
        public int GetMaxCount()
        {
            if (int.TryParse(Max, out int result))
                return result;
            return 0;
        }
    }
}