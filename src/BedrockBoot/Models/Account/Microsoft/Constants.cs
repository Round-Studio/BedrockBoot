using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Account.Microsoft;

public static class Constants
{
    public const string MsaClientId = "0000000048183522";
    public const string MsaScope = "service::user.auth.xboxlive.com::MBI_SSL";
    public const string MsaConnectUrl = "https://login.live.com/oauth20_connect.srf";
    public const string MsaTokenUrl = "https://login.live.com/oauth20_token.srf";
    
    public const string XboxUserAuthEndpoint = "https://user.auth.xboxlive.com/user/authenticate";
    public const string XstsAuthEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";
    
    public const string PeopleHubEndpoint = "https://peoplehub.xboxlive.com/users/xuid({0})/people/social";
    public const string ProfileEndpoint = "https://profile.xboxlive.com/users/xuid({0})/profile/settings?settings=Gamertag,GameDisplayPicRaw";
    
    public const string SisuAuthorizeEndpoint = "https://sisu.xboxlive.com/authorize";
    public const string SisuRelyingParty = "https://b980a380.minecraft.playfabapi.com/";
    
    public const string RedirectUri = "http://127.0.0.1:58423";
}