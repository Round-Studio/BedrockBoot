namespace BedrockBoot.Models.Account.Microsoft;

public class Constants
{
    public const string ClientId = "d46ff04f-3418-49fd-beb8-d2028ff71ff7";
    public const string RedirectUri = "http://localhost";
    public const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    public const string XboxUserAuthEndpoint = "https://user.auth.xboxlive.com/user/authenticate";
    public const string XstsAuthEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";

    public const string PeopleHubEndpoint = "https://peoplehub.xboxlive.com/users/xuid({0})/people/social";

    public const string AuthResultFile = "auth_result.json";
    public const string FriendsResponseFile = "friends_response.json";
}