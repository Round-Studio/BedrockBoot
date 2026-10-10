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

using Microsoft.Identity.Client;
#if !ANDROID
using Microsoft.Identity.Client.Broker;
#endif

namespace BedrockBoot.MSAL;

/// <summary>
/// Windows WAM (MSAL) 登录与令牌刷新封装。
/// 全进程复用同一个 <see cref="IPublicClientApplication"/>，以保证 MSAL 令牌缓存一致。
/// </summary>
public class MSALCore
{
    private const string ClientId = "0000000048183522";
    private const string RedirectUri = "ms-xal-0000000048183522://auth";
    private static readonly string[] Scopes = { "service::user.auth.xboxlive.com::MBI_SSL" };

    private static readonly Lazy<IPublicClientApplication> App = new(CreateApp, isThreadSafe: true);
    private static nint _hwnd;

    private static IPublicClientApplication CreateApp() =>
        PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
            .WithRedirectUri(RedirectUri)
#if ANDROID
            .WithLogging((level, msg, containsPii) => { Console.WriteLine($"[MSAL][{level}] {msg}"); },
                LogLevel.Verbose, enablePiiLogging: false)
#else
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithLogging((level, msg, containsPii) => { Console.WriteLine($"[MSAL][{level}] {msg}"); },
                LogLevel.Verbose, enablePiiLogging: false)
#endif
            .Build();

    public MSALCore(nint hwnd = 0)
    {
        if (hwnd != nint.Zero) _hwnd = hwnd;
    }

    private static IPublicClientApplication Pca => App.Value;

    /// <summary>记录用于交互式登录的父窗口句柄。</summary>
    public static void SetWindowHandle(nint hwnd)
    {
        if (hwnd != nint.Zero) _hwnd = hwnd;
    }

    /// <summary>
    /// 登录：优先静默获取已缓存账户的令牌，失败则走 WAM 交互式登录。
    /// </summary>
    public async Task<AuthenticationResult?> LoginAsync()
    {
        var account = await GetAccountAsync().ConfigureAwait(false);

        if (account != null)
        {
            try
            {
                var silent = await Pca.AcquireTokenSilent(Scopes, account).ExecuteAsync().ConfigureAwait(false);
                Console.WriteLine($"静默登录成功: {silent.Account?.Username}");
                return silent;
            }
            catch (MsalUiRequiredException)
            {
                Console.WriteLine("静默失败，走交互式登录");
            }
        }

        return await AcquireTokenInteractiveAsync().ConfigureAwait(false);
    }

    /// <summary>交互式登录（WAM）。</summary>
    public async Task<AuthenticationResult?> AcquireTokenInteractiveAsync()
    {
        var builder = Pca.AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount);

        if (_hwnd != nint.Zero)
            builder = builder.WithParentActivityOrWindow(_hwnd);

        var result = await builder.ExecuteAsync().ConfigureAwait(false);
        Console.WriteLine($"交互式登录成功: {result.Account?.Username}");
        return result;
    }

    /// <summary>
    /// 静默刷新令牌。找不到对应账户或需要交互时返回 null / 抛出 <see cref="MsalUiRequiredException"/>。
    /// </summary>
    public async Task<AuthenticationResult?> AcquireTokenSilentAsync(string? homeAccountId = null,
        bool forceRefresh = false)
    {
        var account = await GetAccountAsync(homeAccountId).ConfigureAwait(false);
        if (account == null)
        {
            Console.WriteLine("MSAL 缓存中无可用账户");
            return await AcquireTokenInteractiveAsync();
        }

        var result = await Pca.AcquireTokenSilent(Scopes, account)
            .WithForceRefresh(forceRefresh)
            .ExecuteAsync()
            .ConfigureAwait(false);

        Console.WriteLine($"刷新成功，过期时间: {result.ExpiresOn.LocalDateTime}");
        return result;
    }

    /// <summary>获取 MSAL 缓存中的账户，可按 HomeAccountId 精确定位。</summary>
    public async Task<IAccount?> GetAccountAsync(string? homeAccountId = null)
    {
        var accounts = await Pca.GetAccountsAsync().ConfigureAwait(false);
        if (accounts.Count() == 0) return null;

        if (string.IsNullOrEmpty(homeAccountId))
            return accounts.FirstOrDefault();

        return accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == homeAccountId);
    }

    public async Task LogoutAsync()
    {
        foreach (var acc in await Pca.GetAccountsAsync().ConfigureAwait(false))
        {
            await Pca.RemoveAsync(acc).ConfigureAwait(false);
        }
    }
}