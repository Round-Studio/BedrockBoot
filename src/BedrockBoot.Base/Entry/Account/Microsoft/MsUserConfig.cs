using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Account.Microsoft;

public class MsUserConfig
{
    [JsonPropertyName("buid")] public string BUID { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("msAuth")] public XboxAuthEntry.AuthResult? AuthResult { get; set; }
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("userIconUrl")] public string? UserIconUrl { get; set; }
}

public class MsUserConfigRoot
{
    [JsonPropertyName("accounts")] public List<MsUserConfig> Accounts { get; set; } = new();
    [JsonPropertyName("selectUserBuid")] public string? SelectUserBUID { get; set; }
}