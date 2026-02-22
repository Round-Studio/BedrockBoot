namespace BedrockBoot.Base.Entry.Info.Xbox;

public class XboxUserInfo
{
    public string XUID { get; set; }
    public string Gamertag { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public DateTime? TokenExpiration { get; set; }
}