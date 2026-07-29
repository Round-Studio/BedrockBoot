using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;

namespace BedrockBoot.Views.Windows.SubWindows;

public partial class XboxWindow : Window
{
    private static XboxWindow? _instance;

    public XboxWindow()
    {
        InitializeComponent();
        this.Closed += (s, e) => _instance = null;

        LoadAccount();
    }

    private async Task LoadAccount()
    {
        if (!Models.Global.GlobalModel.IsProgressRunning) return;
        var oauth = new MicrosoftOAuthClient();
        var xbox = new XboxAuthClient();
        var people = new PeopleHubClient();

        try
        {
            string? authCode = null;
            string? codeVerifier = null;
            XboxAuthEntry.OAuthTokenResponse? oauthTokens = null;

            var savedAuth = oauth.LoadAuthResult();
            if (savedAuth != null && !string.IsNullOrEmpty(savedAuth.RefreshToken))
            {
                oauthTokens = await oauth.RefreshAccessTokenAsync(savedAuth.RefreshToken);
                if (oauthTokens != null)
                {
                    savedAuth.RefreshToken = oauthTokens.RefreshToken;
                    oauth.SaveAuthResult(savedAuth);
                    Console.WriteLine(@"使用 Refresh Token 自动续期成功");
                }
            }

            if (oauthTokens == null && savedAuth != null && !string.IsNullOrEmpty(savedAuth.Code))
            {
                authCode = savedAuth.Code;
                codeVerifier = savedAuth.CodeVerifier;
                oauthTokens = await oauth.ExchangeCodeForTokensAsync(authCode, codeVerifier);
                if (oauthTokens != null)
                    Console.WriteLine(@"使用保存的授权码重新换取 Token 成功");
            }

            if (oauthTokens == null)
            {
                Console.WriteLine(@"需要完整授权流程，请在浏览器中完成授权。");
                (authCode, codeVerifier) = await oauth.GetAuthorizationCodeAsync();
                if (string.IsNullOrEmpty(authCode))
                {
                    Console.WriteLine(@"获取授权码失败");
                    return;
                }

                oauthTokens = await oauth.ExchangeCodeForTokensAsync(authCode, codeVerifier);
                if (oauthTokens == null)
                {
                    Console.WriteLine(@"换取 OAuth Token 失败");
                    return;
                }

                oauth.SaveAuthResult(new XboxAuthEntry.AuthResult
                {
                    Code = authCode,
                    CodeVerifier = codeVerifier,
                    RedirectUri = Constants.RedirectUri,
                    ClientId = Constants.ClientId,
                    AccessToken = oauthTokens.AccessToken,
                    RefreshToken = oauthTokens.RefreshToken,
                    ExpiresIn = oauthTokens.ExpiresIn,
                    SavedAt = DateTime.Now
                });
            }

            Console.WriteLine(
                $@"获取 Access Token 成功 (前20字符): {oauthTokens.AccessToken?.Substring(0, Math.Min(20, oauthTokens.AccessToken.Length))}...");

            string? xboxUserToken = await xbox.GetXboxUserTokenAsync(oauthTokens.AccessToken);
            if (string.IsNullOrEmpty(xboxUserToken))
            {
                Console.WriteLine(@"换取 Xbox User Token 失败");
                return;
            }

            Console.WriteLine(
                $@"获取 Xbox User Token 成功 (前20字符): {xboxUserToken.Substring(0, Math.Min(20, xboxUserToken.Length))}...");

            var (xstsToken, userHash, xuid) = await xbox.GetXstsTokenAsync(xboxUserToken);
            if (string.IsNullOrEmpty(xstsToken))
            {
                Console.WriteLine(@"换取 XSTS Token 失败");
                return;
            }

            Console.WriteLine(
                $@"获取 XSTS Token 成功 (前20字符): {xstsToken.Substring(0, Math.Min(20, xstsToken.Length))}...");
            Console.WriteLine($@"用户哈希 (UHS): {userHash}");
            Console.WriteLine($@"用户 XUID: {xuid}");

            string authHeader = $"XBL3.0 x={userHash};{xstsToken}";
            var peoples = await people.GetFriendsListAsync(authHeader, xuid);
            LoadProgress.IsVisible = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"发生错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    public static void ShowOrActivate()
    {
        if (_instance == null)
        {
            _instance = new XboxWindow();
            _instance.Show();
        }
        else
        {
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }

    private void CloseBorderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}