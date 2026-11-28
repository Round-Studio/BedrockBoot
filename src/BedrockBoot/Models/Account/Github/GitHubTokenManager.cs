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
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Github;

/// <summary>
///     GitHub 返回的一组令牌。长期令牌（未开启短效令牌）的过期时间为 <c>null</c>，
///     此时不会尝试刷新，行为与改动前一致。
/// </summary>
public sealed record GitHubTokenSet(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RefreshTokenExpiresAt,
    string? Scope);

/// <summary>
///     没有可用的刷新凭证，或 refresh_token 已被撤销/过期，只能让用户重新授权。
///     抛出前本地令牌已被清空，账户页会回到未登录状态。
/// </summary>
public sealed class GitHubReauthRequiredException : Exception
{
    public GitHubReauthRequiredException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

/// <summary>
///     GitHub 令牌的读取、续期与落盘。
///     <para>
///     OAuth App 开启短效令牌后，access_token 只有 8 小时，refresh_token 6 个月且<b>一次性</b>：
///     每刷新一次都会换一对新令牌，旧的立刻失效。因此刷新必须串行——并发刷新会让后到的请求
///     拿着已被用掉的 refresh_token 去换，直接报 <c>bad_refresh_token</c> 把用户踢下线。
///     </para>
/// </summary>
public static class GitHubTokenManager
{
    private const string TokenEndpoint = "https://github.com/login/oauth/access_token";
    private const string ProductHeader = "BedrockBoot";

    /// <summary>refresh_token 一次性，所有刷新排同一把锁串行执行</summary>
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    /// <summary>明文配置 → 密文的迁移每个进程只尝试一次</summary>
    private static int _migrationAttempted;

    /// <summary>提前 5 分钟续期，避免请求真正发出时令牌刚好过期</summary>
    private static readonly TimeSpan ExpiryLeeway = TimeSpan.FromMinutes(5);

    /// <summary>只读本地授权信息，不触发刷新、不创建文件。凭证字段在内存里始终是明文。</summary>
    public static GithubAccountInfo? ReadAccount()
    {
        try
        {
            var data = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount, false).Data;
            if (data is null) return null;

            // 旧版本配置里是明文，先记住"需要迁移"，再统一还原成明文供调用方使用
            var needsMigration = HasPlaintextCredential(data);

            data.UserToken = SecretProtector.Unprotect(data.UserToken) ?? string.Empty;
            data.RefreshToken = SecretProtector.Unprotect(data.RefreshToken);

            if (needsMigration) MigrateToProtected(data);

            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取 GitHub 授权信息失败：{ex.Message}");
            return null;
        }
    }

    /// <summary>写入账户信息（登录成功后调用）：只加密两个凭证字段，其余照常可读</summary>
    public static void SaveAccount(GithubAccountInfo account)
    {
        var config = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount);
        config.Data = new GithubAccountInfo
        {
            UserImage = account.UserImage,
            UserName = account.UserName,
            UserToken = SecretProtector.Protect(account.UserToken) ?? string.Empty,
            RefreshToken = SecretProtector.Protect(account.RefreshToken),
            ExpiresAt = account.ExpiresAt,
            RefreshTokenExpiresAt = account.RefreshTokenExpiresAt,
            Scope = account.Scope
        };
        config.Save();
    }

    /// <summary>是否还有明文凭证需要改写成密文（只在当前环境真的能加密时为 true）</summary>
    private static bool HasPlaintextCredential(GithubAccountInfo data)
    {
        if (!SecretProtector.IsAvailable) return false;

        return (!string.IsNullOrEmpty(data.UserToken) && !SecretProtector.IsProtected(data.UserToken)) ||
               (!string.IsNullOrEmpty(data.RefreshToken) && !SecretProtector.IsProtected(data.RefreshToken));
    }

    /// <summary>
    ///     把旧版本的明文配置改写为密文。每个进程只做一次，失败只记日志，
    ///     读出来的明文照样能用，下次保存还会再试。
    /// </summary>
    private static void MigrateToProtected(GithubAccountInfo plainAccount)
    {
        if (Interlocked.Exchange(ref _migrationAttempted, 1) == 1) return;

        try
        {
            SaveAccount(plainAccount);
            Console.WriteLine(@"GitHub 凭证已改为加密保存");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"GitHub 凭证加密迁移失败（继续使用明文配置）：{ex.Message}");
        }
    }

    /// <summary>本地是否存在任何形式的凭证（access 或 refresh）</summary>
    public static bool HasCredential(GithubAccountInfo? account) =>
        account != null &&
        (!string.IsNullOrEmpty(account.UserToken) || !string.IsNullOrEmpty(account.RefreshToken));

    /// <summary>refresh_token 是否存在且未过期</summary>
    public static bool CanRefresh(GithubAccountInfo? account) =>
        account != null &&
        !string.IsNullOrEmpty(account.RefreshToken) &&
        (account.RefreshTokenExpiresAt is not { } expiresAt || expiresAt > DateTimeOffset.UtcNow);

    /// <summary>access_token 是否已过期（无过期时间的长期令牌永远返回 false）</summary>
    public static bool IsAccessTokenExpired(GithubAccountInfo? account, TimeSpan? leeway = null)
    {
        if (account?.ExpiresAt is not { } expiresAt) return false;
        return DateTimeOffset.UtcNow + (leeway ?? TimeSpan.Zero) >= expiresAt;
    }

    /// <summary>
    ///     界面用的登录态：令牌还在有效期内，或者虽然过期但还能自动刷新。
    ///     过期且无法刷新（refresh_token 也没了）视为未登录，引导用户重新授权。
    /// </summary>
    public static bool IsLoggedIn
    {
        get
        {
            var account = ReadAccount();
            if (account is null || !HasCredential(account)) return false;
            if (!string.IsNullOrEmpty(account.UserToken) && !IsAccessTokenExpired(account)) return true;
            return CanRefresh(account);
        }
    }

    /// <summary>
    ///     取得可直接使用的 access_token：快过期先续期，已过期必须续期。
    ///     未登录返回 <c>null</c>；无法续期时抛 <see cref="GitHubReauthRequiredException" />。
    /// </summary>
    /// <param name="forceRefresh">即使还没到期也强制刷新（用于 401 后重试）</param>
    /// <param name="rejectedToken">被服务端拒绝的旧令牌；若别的调用方已经换过令牌则直接复用新令牌</param>
    public static async Task<string?> GetValidTokenAsync(bool forceRefresh = false, string? rejectedToken = null)
    {
        var account = ReadAccount();
        if (account is null || !HasCredential(account)) return null;

        if (CanUseCurrentToken(account, forceRefresh, rejectedToken))
            return account.UserToken;

        return await RefreshSingleFlightAsync(forceRefresh, rejectedToken).ConfigureAwait(false);
    }

    /// <summary>解析 GitHub 的令牌响应；不含 access_token 时返回 <c>null</c></summary>
    public static GitHubTokenSet? TryParseTokenSet(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return null;

        if (!root.TryGetProperty("access_token", out var accessToken) ||
            accessToken.ValueKind != JsonValueKind.String)
            return null;

        var token = accessToken.GetString();
        if (string.IsNullOrEmpty(token)) return null;

        return new GitHubTokenSet(
            token,
            ReadString(root, "refresh_token"),
            ReadExpiry(root, "expires_in"),
            ReadExpiry(root, "refresh_token_expires_in"),
            ReadString(root, "scope"));
    }

    /// <summary>响应里的错误码（<c>bad_refresh_token</c> 等），没有则返回 <c>null</c></summary>
    public static string? ReadErrorCode(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? ReadString(document.RootElement, "error")
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool CanUseCurrentToken(GithubAccountInfo? account, bool forceRefresh, string? rejectedToken)
    {
        if (account is null) return false;

        // 传了被拒绝的令牌：只要本地已经不是它，说明别的调用方刚刷新过，直接用新的
        if (rejectedToken != null)
            return !string.IsNullOrEmpty(account.UserToken) && account.UserToken != rejectedToken;

        if (forceRefresh) return false;

        return !string.IsNullOrEmpty(account.UserToken) && !IsAccessTokenExpired(account, ExpiryLeeway);
    }

    private static async Task<string?> RefreshSingleFlightAsync(bool forceRefresh, string? rejectedToken)
    {
        await RefreshLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // 等锁期间状态可能已经变了（别的调用方刷新成功、或用户退出了登录），必须重新读盘
            var account = ReadAccount();
            if (account is null || !HasCredential(account)) return null;

            if (CanUseCurrentToken(account, forceRefresh, rejectedToken))
                return account.UserToken;

            var refreshToken = account.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken) || !CanRefresh(account))
            {
                // 没有可用的刷新凭证。forceRefresh 说明服务端已经拒绝了这个令牌，
                // 继续留着只会让界面显示"已登录"却什么都读不到，直接清掉引导重新登录
                if (forceRefresh || IsAccessTokenExpired(account))
                {
                    ClearCredential();
                    throw new GitHubReauthRequiredException("GitHub 登录已过期，且没有可用的刷新凭证，需要重新授权。");
                }

                return account.UserToken;
            }

            try
            {
                var tokens = await RequestRefreshAsync(refreshToken).ConfigureAwait(false);
                SaveTokens(tokens);
                Console.WriteLine(
                    $@"GitHub 令牌已刷新，新令牌有效期至 {tokens.ExpiresAt?.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
                return tokens.AccessToken;
            }
            catch (GitHubReauthRequiredException)
            {
                // refresh_token 被撤销或过期，本地凭证已经没有意义，清掉让界面回到登录入口
                ClearCredential();
                throw;
            }
            catch (Exception ex)
            {
                // 断网等临时失败：当前令牌还有效就继续用；已经过期则把原始错误抛出去，
                // 让用户看到网络错误而不是被误判成"登录失效"，下次请求还会再尝试刷新
                if (IsAccessTokenExpired(account))
                {
                    Console.WriteLine($@"GitHub 令牌刷新失败且令牌已过期：{ex.Message}");
                    throw;
                }

                Console.WriteLine($@"GitHub 令牌刷新失败（继续使用当前令牌）：{ex.Message}");
                return account.UserToken;
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<GitHubTokenSet> RequestRefreshAsync(string refreshToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", BedrockBoot.Core.Keys.GithubClientId),
            new KeyValuePair<string, string>("client_secret", BedrockBoot.Core.Keys.GithubSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint) { Content = content };
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("User-Agent", ProductHeader);

        using var response = await http.SendAsync(request).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var tokens = TryParseTokenSet(json);
        if (tokens != null) return tokens;

        var error = ReadErrorCode(json);
        if (string.Equals(error, "bad_refresh_token", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase))
            throw new GitHubReauthRequiredException("GitHub 刷新凭证已失效，需要重新授权。");

        throw new InvalidOperationException(
            $"GitHub 令牌刷新失败（HTTP {(int)response.StatusCode}）：{json}");
    }

    /// <summary>把刷新结果写回配置；刷新期间用户退出登录时什么都不做</summary>
    private static void SaveTokens(GitHubTokenSet tokens)
    {
        try
        {
            if (!File.Exists(PathsList.GithubAccount)) return;

            var config = LoadForWrite();
            if (config.Data is null || !HasCredential(config.Data)) return;

            config.Data.UserToken = tokens.AccessToken;
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
                config.Data.RefreshToken = tokens.RefreshToken;
            config.Data.ExpiresAt = tokens.ExpiresAt;
            if (tokens.RefreshTokenExpiresAt is not null)
                config.Data.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
            if (!string.IsNullOrEmpty(tokens.Scope))
                config.Data.Scope = tokens.Scope;

            SaveProtected(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"保存 GitHub 令牌失败：{ex.Message}");
        }
    }

    /// <summary>清空令牌但保留昵称头像，账户页据此回到未登录状态</summary>
    private static void ClearCredential()
    {
        try
        {
            if (!File.Exists(PathsList.GithubAccount)) return;

            var config = LoadForWrite();
            if (config.Data is null) return;

            config.Data.UserToken = string.Empty;
            config.Data.RefreshToken = null;
            config.Data.ExpiresAt = null;
            config.Data.RefreshTokenExpiresAt = null;
            config.Data.Scope = null;
            SaveProtected(config);

            Console.WriteLine(@"GitHub 凭证已失效，已清空本地令牌，需要重新登录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"清空 GitHub 令牌失败：{ex.Message}");
        }
    }

    /// <summary>读盘并把凭证还原成明文，供"读-改-写"流程使用（直接改密文会导致再次加密）</summary>
    private static ConfigEntity<GithubAccountInfo> LoadForWrite()
    {
        var config = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount);
        if (config.Data is not null)
        {
            config.Data.UserToken = SecretProtector.Unprotect(config.Data.UserToken) ?? string.Empty;
            config.Data.RefreshToken = SecretProtector.Unprotect(config.Data.RefreshToken);
        }

        return config;
    }

    /// <summary>加密凭证字段后落盘；其余字段保持明文，方便排查</summary>
    private static void SaveProtected(ConfigEntity<GithubAccountInfo> config)
    {
        if (config.Data is not null)
        {
            config.Data.UserToken = SecretProtector.Protect(config.Data.UserToken) ?? string.Empty;
            config.Data.RefreshToken = SecretProtector.Protect(config.Data.RefreshToken);
        }

        config.Save();
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>GitHub 用秒数表示有效期，可能以数字或字符串返回</summary>
    private static DateTimeOffset? ReadExpiry(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;

        long seconds;
        switch (value.ValueKind)
        {
            case JsonValueKind.Number when value.TryGetInt64(out var number):
                seconds = number;
                break;
            case JsonValueKind.String when long.TryParse(value.GetString(), out var parsed):
                seconds = parsed;
                break;
            default:
                return null;
        }

        return seconds <= 0 ? null : DateTimeOffset.UtcNow.AddSeconds(seconds);
    }
}
