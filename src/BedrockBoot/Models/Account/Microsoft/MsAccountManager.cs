using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Microsoft;

public static class MsAccountManager
{
    public static ConfigEntity<MsUserConfigRoot>? AccountConfigEntity;

    public static MsUserConfigRoot? Accounts
    {
        get => AccountConfigEntity?.Data;
    }

    public static bool IsLogging { get; private set; } = false;

    static MsAccountManager()
    {
        AccountConfigEntity = new ConfigEntity<MsUserConfigRoot>(PathsList.MsAccountPath);
        AccountConfigEntity.Load();
    }

    public static async Task LoginAccount()
    {
        if (IsLogging) throw new Exception("已有登录任务进行中");
        IsLogging = true;

        DialogHost.Show(new()
        {
            Content = new DialogLoginMsAccountContent(),
            Title = "关联 XBOX 账户",
            CloseButtonText = "取消",
            CloseAction = () => { }
        });

        var client = new MicrosoftOAuthClient();
        Console.WriteLine("开始关联微软账户");
        var account = await client.GetAuthorizationCodeAsync();
        if (account.authCode == null ||
            account.codeVerifier == null) throw new NullReferenceException();
        Console.WriteLine("开始获取微软账户登录凭证");
        var exchangeCode = await client.ExchangeCodeForTokensAsync(account.authCode, account.codeVerifier);
        if (exchangeCode == null) throw new NullReferenceException();

        Console.WriteLine("开始获取 Xbox 用户凭证");
        var xboxClient = new XboxAuthClient();
        var xboxUserToken = await xboxClient.GetXboxUserTokenAsync(exchangeCode.AccessToken);
        if (string.IsNullOrEmpty(xboxUserToken)) throw new NullReferenceException();
        Console.WriteLine("开始获取 Xbox 用户登录凭证 (XstsToken)");
        var xstsToken = await xboxClient.GetXstsTokenAsync(xboxUserToken);
        if (string.IsNullOrEmpty(xstsToken.xstsToken) ||
            string.IsNullOrEmpty(xstsToken.xuid) ||
            string.IsNullOrEmpty(xstsToken.userHash)) throw new NullReferenceException();

        Console.WriteLine("开始获取 Xbox 用户档案");
        var peopleClient = new PeopleHubClient();
        string authHeader = $"XBL3.0 x={xstsToken.userHash};{xstsToken.xstsToken}";
        var userProfile = await peopleClient.GetProfileAsync(authHeader, xstsToken.xuid);
        if (userProfile == null) throw new NullReferenceException();
        var userInfo = userProfile.ProfileUsers[0];

        var config = new MsUserConfig()
        {
            AuthResult = new()
            {
                AccessToken = exchangeCode.AccessToken,
                RefreshToken = exchangeCode.RefreshToken,
                ExpiresIn = exchangeCode.ExpiresIn,
                SavedAt = DateTime.Now
            },
            UserName = userInfo.Settings.FirstOrDefault(s => s.Id == "Gamertag")?.Value,
            UserIconUrl = userInfo.Settings?.FirstOrDefault(s => s.Id == "GameDisplayPicRaw")?.Value
        };
        AccountConfigEntity?.Data.Accounts.Add(config);
        if (string.IsNullOrEmpty(AccountConfigEntity!.Data.SelectUserBUID))
            AccountConfigEntity.Data.SelectUserBUID = config.BUID;
        AccountConfigEntity?.Save();

        IsLogging = false;
        _ = DialogHost.Close();
    }
}