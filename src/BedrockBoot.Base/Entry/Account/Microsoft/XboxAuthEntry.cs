using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Account.Microsoft;

public class XboxAuthEntry
{
    #region 数据模型

    public class AuthResult
    {
        [JsonPropertyName("code")] public string? Code { get; set; }

        [JsonPropertyName("code_verifier")] public string? CodeVerifier { get; set; }

        [JsonPropertyName("redirect_uri")] public string? RedirectUri { get; set; }

        [JsonPropertyName("client_id")] public string? ClientId { get; set; }

        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

        [JsonPropertyName("saved_at")] public DateTime SavedAt { get; set; }
    }

    public class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")] public string? IdToken { get; set; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")] public string? TokenType { get; set; }

        [JsonPropertyName("scope")] public string? Scope { get; set; }
    }

    public class XboxAuthResponse
    {
        [JsonPropertyName("Token")] public string? Token { get; set; }

        [JsonPropertyName("DisplayClaims")] public DisplayClaims? DisplayClaims { get; set; }
    }

    public class DisplayClaims
    {
        [JsonPropertyName("xui")] public XuiClaim[]? xui { get; set; }
    }

    public class XuiClaim
    {
        [JsonPropertyName("uhs")] public string? uhs { get; set; }

        [JsonPropertyName("xid")] public string? xid { get; set; }
    }

    public class XboxErrorResponse
    {
        [JsonPropertyName("XErr")] public long? XErr { get; set; }
    }

    public class PeopleHubResponse
    {
        [JsonPropertyName("people")] public PeoplePerson[]? People { get; set; }

        [JsonPropertyName("totalCount")] public int TotalCount { get; set; }

        [JsonPropertyName("filter")] public string? Filter { get; set; }
    }

    public class PeoplePerson
    {
        [JsonPropertyName("xuid")] public string? Xuid { get; set; }

        [JsonPropertyName("gamertag")] public string? Gamertag { get; set; }

        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }

        [JsonPropertyName("realName")] public string? RealName { get; set; }

        [JsonPropertyName("hasXboxLiveProfile")]
        public bool HasXboxLiveProfile { get; set; }

        [JsonPropertyName("isFavorite")] public bool IsFavorite { get; set; }

        [JsonPropertyName("isFollowingCaller")]
        public bool IsFollowingCaller { get; set; }

        [JsonPropertyName("followerCount")] public int FollowerCount { get; set; }

        [JsonPropertyName("followingCount")] public int FollowingCount { get; set; }

        [JsonPropertyName("presence")] public PresenceInfo? Presence { get; set; }
    }

    public class PresenceInfo
    {
        [JsonPropertyName("state")] public string? State { get; set; }

        [JsonPropertyName("presence")] public string[]? Presence { get; set; }
    }

    #endregion

    public class XboxProfileResponse
    {
        [JsonPropertyName("profileUsers")] public ProfileUser[]? ProfileUsers { get; set; }
    }

    public class ProfileUser
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("hostId")] public string HostId { get; set; }
        [JsonPropertyName("settings")] public ProfileSetting[] Settings { get; set; }
    }

    public class ProfileSetting
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("value")] public string Value { get; set; }
    }
}