/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Global;
using BedrockBoot.MSAL;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Windows;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Microsoft;

public static class MsAccountManager
{
    public static ConfigEntity<MsUserConfigRoot>? AccountConfigEntity;

    public static MsUserConfigRoot? Accounts => AccountConfigEntity?.Data;

    public static bool IsLogging { get; private set; } = false;

    private static Task OnUI(Action action) =>
        Dispatcher.UIThread.InvokeAsync(action).GetTask();

    private static Task<T> OnUI<T>(Func<T> func) =>
        Dispatcher.UIThread.InvokeAsync(func).GetTask();

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

        await OnUI(() =>
        {
            DialogHost.Show(new()
            {
                Content = dialog,
                Title = "关联 XBOX 账户",
                CloseButtonText = "取消",
                CloseAction = () => { IsLogging = false; }
            });
        });

        try
        {
            await Task.Run(async () =>
            {
                string? accessToken = null;
                string? refreshToken = null;
                int expiresIn = 3600;
                bool isMsal = false;
                string? msalHomeAccountId = null;

                if (OperatingSystem.IsWindows() && Core.Global.GlobalModel.Config.Data.IsUseMSALAccount)
                {
#if WINDOWS
                    Console.WriteLine(@"开始 WAM 交互式登录...");

                    var hwnd = GlobalModel.MainWindow.TryGetPlatformHandle()?.Handle ?? 0;
                    var msal = new MSALCore(hwnd);
                    var result = await msal.AcquireTokenInteractiveAsync().ConfigureAwait(false);

                    if (result == null || string.IsNullOrEmpty(result.AccessToken))
                        throw new Exception("登录失败或用户取消");

                    accessToken = result.AccessToken;
                    refreshToken = null;
                    expiresIn = (int)(result.ExpiresOn - DateTimeOffset.Now).TotalSeconds;

                    if (expiresIn <= 0) expiresIn = 3600;

                    // 标记为 MSAL 账户：此类账户没有 refresh_token，刷新必须走 MSAL
                    isMsal = true;
                    msalHomeAccountId = result.Account?.HomeAccountId?.Identifier;
#endif
                }
                else
                {
                    Console.WriteLine(@"开始设备代码登录流程...");

                    var client = new MsaDeviceCodeClient();

                    client.OnLoginCallback = (userCode, verificationUri) =>
                    {
                        _ = OnUI(() => dialog.SetCopyCode(verificationUri, userCode));
                    };

                    var progress = new Progress<string>(msg => { _ = OnUI(() => Console.WriteLine(msg)); });

                    using var cts = new CancellationTokenSource();

                    var (success, tokenData, userCode, verificationUri) =
                        await client.RunDeviceCodeFlowAsync(progress, cts.Token)
                            .ConfigureAwait(false);

                    if (!success || tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                        throw new Exception("登录失败或用户取消");

                    accessToken = tokenData.AccessToken;
                    refreshToken = tokenData.RefreshToken;
                    expiresIn = tokenData.ExpiresIn ?? 3600;
                }

                Console.WriteLine(@"开始获取 Xbox 用户凭证");
                var xboxClient = new XboxAuthClient();
                var xboxUserToken = await xboxClient
                    .GetXboxUserTokenAsync(accessToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(xboxUserToken))
                    throw new NullReferenceException("获取 Xbox 用户凭证失败");

                Console.WriteLine(@"开始获取 Xbox 用户登录凭证 (XstsToken)");
                var xstsToken = await xboxClient
                    .GetXstsTokenAsync(xboxUserToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(xstsToken.xstsToken) ||
                    string.IsNullOrEmpty(xstsToken.xuid) ||
                    string.IsNullOrEmpty(xstsToken.userHash))
                    throw new NullReferenceException("获取 XSTS Token 失败");

                Console.WriteLine(@"开始获取 Xbox 用户档案");
                var peopleClient = new PeopleHubClient();
                string authHeader = $"XBL3.0 x={xstsToken.userHash};{xstsToken.xstsToken}";
                var userProfile = await peopleClient
                    .GetProfileAsync(authHeader, xstsToken.xuid)
                    .ConfigureAwait(false);

                if (userProfile?.ProfileUsers == null || userProfile.ProfileUsers.Length == 0)
                    throw new NullReferenceException("获取用户档案失败");

                var userInfo = userProfile.ProfileUsers[0];
                var gamertag = userInfo.Settings?.FirstOrDefault(s => s.Id == "Gamertag")?.Value;
                var avatarUrl = userInfo.Settings?.FirstOrDefault(s => s.Id == "GameDisplayPicRaw")?.Value;

                var config = new MsUserConfig
                {
                    AuthResult = new XboxAuthEntry.AuthResult
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken ?? "",
                        ExpiresIn = expiresIn,
                        SavedAt = DateTime.Now,
                    },
                    UserName = gamertag,
                    UserIconUrl = avatarUrl,
                    IsMsal = isMsal,
                    MsalHomeAccountId = msalHomeAccountId
                };

                await OnUI(() =>
                {
                    AccountConfigEntity?.Data.Accounts.Add(config);

                    if (string.IsNullOrEmpty(AccountConfigEntity?.Data.SelectUserBUID))
                        AccountConfigEntity!.Data.SelectUserBUID = config.BUID;

                    AccountConfigEntity?.Save();
                }).ConfigureAwait(false);

                Console.WriteLine($@"登录成功！用户: {gamertag}");
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"登录失败: {ex.Message}");
            throw;
        }
        finally
        {
            await OnUI(() =>
            {
                IsLogging = false;
                _ = DialogHost.Close();
            }).ConfigureAwait(false);
        }
    }

    public static async Task<bool> RefreshAllTokens()
    {
        if (AccountConfigEntity?.Data?.Accounts == null) return false;

        return await Task.Run(async () =>
        {
            var client = new MsaDeviceCodeClient();
            bool anyRefreshed = false;

            foreach (var account in AccountConfigEntity.Data.Accounts)
            {
                if (account.AuthResult == null) continue;

                // MSAL 账户没有 refresh_token，不能因 refresh_token 为空而跳过
                if (!account.IsMsal && string.IsNullOrEmpty(account.AuthResult.RefreshToken)) continue;

                var newToken = await client.RefreshTokenAsync(account)
                    .ConfigureAwait(false);
                if (newToken != null)
                {
                    ApplyToken(account, newToken);
                    anyRefreshed = true;
                }
            }

            if (anyRefreshed)
            {
                await OnUI(() => AccountConfigEntity?.Save()).ConfigureAwait(false);
            }

            return anyRefreshed;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// 刷新指定账户的凭证并写回配置。成功返回刷新后的账户，失败返回 null。
    /// </summary>
    public static async Task<MsUserConfig?> RefreshAccountAsync(MsUserConfig account)
    {
        var client = new MsaDeviceCodeClient();
        var newToken = await client.RefreshTokenAsync(account).ConfigureAwait(false);
        if (newToken == null) return null;

        ApplyToken(account, newToken);
        await OnUI(() => AccountConfigEntity?.Save()).ConfigureAwait(false);
        return account;
    }

    private static void ApplyToken(MsUserConfig account, MsaDeviceCodeClient.TokenData token)
    {
        account.AuthResult ??= new XboxAuthEntry.AuthResult();
        account.AuthResult.AccessToken = token.AccessToken;
        account.AuthResult.RefreshToken = token.RefreshToken;
        account.AuthResult.ExpiresIn = token.ExpiresIn ?? 3600;
        account.AuthResult.SavedAt = DateTime.Now;

        if (token.IsMsal)
        {
            account.IsMsal = true;
            account.MsalHomeAccountId = token.MsalHomeAccountId ?? account.MsalHomeAccountId;
        }
    }

    public static async Task<string?> GetValidAccessToken(string buid)
    {
        var account = AccountConfigEntity?.Data?.Accounts.FirstOrDefault(a => a.BUID == buid);
        if (account?.AuthResult == null) return null;

        if (account.AuthResult.SavedAt.AddSeconds(account.AuthResult.ExpiresIn - 300) < DateTime.Now)
        {
            return await Task.Run(async () =>
            {
                var newToken = await RefreshAccountAsync(account).ConfigureAwait(false);
                return newToken?.AuthResult?.AccessToken;
            }).ConfigureAwait(false);
        }

        return account.AuthResult.AccessToken;
    }
}