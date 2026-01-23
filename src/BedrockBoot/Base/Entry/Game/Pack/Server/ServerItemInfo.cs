using System.Collections.Generic;

namespace BedrockBoot.Base.Entry.Game.Pack.Server;
public class ServerItemInfo
{
    public string ServerName { get; set; } = "第三方服务器";
    public required string ServerAddress { get; set; }
    public int ServerPort { get; set; } = 19132;
}