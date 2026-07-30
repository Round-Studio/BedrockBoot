using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        AccountConfigEntity.AfterSave += (_, _) =>
        {
            if (AccountConfigEntity.Data.SelectUserBUID != null &&
                AccountConfigEntity.Data.Accounts.Count >= 1)
            {
                var buids = AccountConfigEntity.Data.Accounts.Select(x => x.BUID);
                if (!buids.Contains(AccountConfigEntity.Data.SelectUserBUID))
                {
                    AccountConfigEntity.Data.SelectUserBUID = buids.ToArray()[0];
                    AccountConfigEntity.Save();
                }
            }
        };
    }

    public static async Task LoginAccount()
    {
        if (IsLogging) throw new Exception("已有登录任务进行中");
        IsLogging = true;

        var dialog = new DialogLoginMsAccountContent();

        DialogHost.Show(new()
        {
            Content = dialog,
            Title = "关联 XBOX 账户",
            CloseButtonText = "取消",
            CloseAction = () =>
            {
                IsLogging = false;
            }
        });

        try
        {
            var client = new MsaDeviceCodeClient();
            
            Console.WriteLine("开始设备代码登录流程...");

            client.OnLoginCallback = (s, s1) =>
                dialog.SetCopyCode(s1, s);
            var progress = new Progress<string>(msg => 
            {
                Console.WriteLine(msg);
            });

            var cancellationToken = new CancellationTokenSource();
            
            var (success, tokenData, userCode, verificationUri) = 
                await client.RunDeviceCodeFlowAsync(progress, cancellationToken.Token);

            if (!success || tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                IsLogging = false;
                await DialogHost.Close();
                throw new Exception("登录失败或用户取消");
            }

            Console.WriteLine("开始获取 Xbox 用户凭证");
            var xboxClient = new XboxAuthClient();
            var xboxUserToken = await xboxClient.GetXboxUserTokenAsync(tokenData.AccessToken);
            
            if (string.IsNullOrEmpty(xboxUserToken))
            {
                IsLogging = false;
                await DialogHost.Close();
                throw new NullReferenceException("获取 Xbox 用户凭证失败");
            }

            Console.WriteLine("开始获取 Xbox 用户登录凭证 (XstsToken)");
            var xstsToken = await xboxClient.GetXstsTokenAsync(xboxUserToken);
            
            if (string.IsNullOrEmpty(xstsToken.xstsToken) ||
                string.IsNullOrEmpty(xstsToken.xuid) ||
                string.IsNullOrEmpty(xstsToken.userHash))
            {
                IsLogging = false;
                await DialogHost.Close();
                throw new NullReferenceException("获取 XSTS Token 失败");
            }

            Console.WriteLine("开始获取 Xbox 用户档案");
            var peopleClient = new PeopleHubClient();
            string authHeader = $"XBL3.0 x={xstsToken.userHash};{xstsToken.xstsToken}";
            var userProfile = await peopleClient.GetProfileAsync(authHeader, xstsToken.xuid);
            
            if (userProfile == null)
            {
                IsLogging = false;
                await DialogHost.Close();
                throw new NullReferenceException("获取用户档案失败");
            }

            var userInfo = userProfile.ProfileUsers[0];
            var gamertag = userInfo.Settings.FirstOrDefault(s => s.Id == "Gamertag")?.Value;
            var avatarUrl = userInfo.Settings?.FirstOrDefault(s => s.Id == "GameDisplayPicRaw")?.Value;

            var config = new MsUserConfig
            {
                AuthResult = new XboxAuthEntry.AuthResult
                {
                    AccessToken = tokenData.AccessToken,
                    RefreshToken = tokenData.RefreshToken ?? "",
                    ExpiresIn = tokenData.ExpiresIn ?? 3600,
                    SavedAt = DateTime.Now,
                },
                UserName = gamertag,
                UserIconUrl = avatarUrl
            };

            AccountConfigEntity?.Data.Accounts.Add(config);
            
            if (string.IsNullOrEmpty(AccountConfigEntity?.Data.SelectUserBUID))
                AccountConfigEntity!.Data.SelectUserBUID = config.BUID;
            
            AccountConfigEntity?.Save();

            Console.WriteLine($"登录成功！用户: {gamertag}");

            IsLogging = false;
            await DialogHost.Close();
        }
        catch (Exception ex)
        {
            IsLogging = false;
            await DialogHost.Close();
            Console.WriteLine($"登录失败: {ex.Message}");
            throw;
        }
    }

    public static async Task<bool> RefreshAllTokens()
    {
        if (AccountConfigEntity?.Data?.Accounts == null) return false;

        var client = new MsaDeviceCodeClient();
        bool anyRefreshed = false;

        foreach (var account in AccountConfigEntity.Data.Accounts)
        {
            if (string.IsNullOrEmpty(account.AuthResult?.RefreshToken)) continue;

            var newToken = await client.RefreshTokenAsync(account.AuthResult.RefreshToken);
            if (newToken != null)
            {
                account.AuthResult.AccessToken = newToken.AccessToken;
                account.AuthResult.RefreshToken = newToken.RefreshToken;
                account.AuthResult.ExpiresIn = newToken.ExpiresIn ?? 3600;
                account.AuthResult.SavedAt = DateTime.Now;
                anyRefreshed = true;
            }
        }

        if (anyRefreshed)
            AccountConfigEntity?.Save();

        return anyRefreshed;
    }

    public static async Task<string?> GetValidAccessToken(string buid)
    {
        var account = AccountConfigEntity?.Data?.Accounts.FirstOrDefault(a => a.BUID == buid);
        if (account?.AuthResult == null) return null;

        if (account.AuthResult.SavedAt.AddSeconds(account.AuthResult.ExpiresIn - 300) < DateTime.Now)
        {
            var client = new MsaDeviceCodeClient();
            var newToken = await client.RefreshTokenAsync(account.AuthResult.RefreshToken);
            
            if (newToken != null)
            {
                account.AuthResult.AccessToken = newToken.AccessToken;
                account.AuthResult.RefreshToken = newToken.RefreshToken;
                account.AuthResult.ExpiresIn = newToken.ExpiresIn ?? 3600;
                account.AuthResult.SavedAt = DateTime.Now;
                AccountConfigEntity?.Save();
                return newToken.AccessToken;
            }
            return null;
        }

        return account.AuthResult.AccessToken;
    }
}