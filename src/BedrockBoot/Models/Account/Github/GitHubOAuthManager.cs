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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Entity;
using Octokit;
using OnePointUI.Avalonia.Style.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Github;

public class GitHubOAuthManager
{
    private string _clientId => BedrockBoot.Core.Keys.GithubClientId;
    private string _clientSecret => BedrockBoot.Core.Keys.GithubSecret;

    private const string CallbackPath = "/auth/github/callback";
    private static readonly Random _portRandom = new();

    public async Task<GithubAccountInfo> LoginAsync(bool fullAccess = true)
    {
        using var listener = new HttpListener();

        // 随机端口可能被其他程序占用，逐个候选重试，直到监听成功
        var port = 0;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            port = _portRandom.Next(10000, 49152);
            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                listener.Start();
                break;
            }
            catch (HttpListenerException ex) when (attempt < 7)
            {
                Console.WriteLine($@"OAuth 回调端口 {port} 被占用，换端口重试: {ex.Message}");
            }
        }

        if (!listener.IsListening)
            throw new InvalidOperationException(I18nManager.Instance["Auth.OAuth.ListenerStartFailed"]);

        var redirectUri = $"http://127.0.0.1:{port}{CallbackPath}";
        var state = Guid.NewGuid().ToString("N");
        // offline_access：签发短效 access_token（8 小时）+ refresh_token（6 个月），由 GitHubTokenManager 续期。
        // repo：只有它才能下载 Actions 产物，所以仅在用户选择「完整授权」时才申请；
        // 选择「仅登录」只申请 user:email，能绕过 API 匿名限流，但下载产物会被 403 拒绝。
        var scope = fullAccess
            ? "user:email repo offline_access"
            : "user:email offline_access";
        var authUrl = $"https://github.com/login/oauth/authorize" +
                      $"?client_id={_clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(scope)}" +
                      $"&state={state}";

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var responseHtml = await new JsonResourceEntity()
            .ReadTextResourceAsync("avares://BedrockBoot/Assets/Web/LoginFinish.html");

        var colors = await GetColorsOnUIThreadAsync();

        responseHtml = responseHtml
            .Replace("{BackgroundBrush}", colors.BackgroundBrush)
            .Replace("{PrimaryForegroundBrush}", colors.PrimaryForegroundBrush)
            .Replace("{PrimaryDisabledForegroundBrush}", colors.PrimaryDisabledForegroundBrush)
            .Replace("{PrimaryDisabled2ForegroundBrush}", colors.PrimaryDisabled2ForegroundBrush)
            .Replace("{PrimaryBorderBrush}", colors.PrimaryBorderBrush)
            .Replace("{AccentBorderBrush}", colors.AccentBorderBrush)
            .Replace("{AccentBackgroundBrush}", colors.AccentBackgroundBrush)
            .Replace("{AccentBackgroundOverBrush}", colors.AccentBackgroundOverBrush);

        string code;
        try
        {
            // 等待浏览器回调（最多 5 分钟）。state 校验通过前不回复成功页面，
            // 避免旧版“先回页后校验”在无效回调时泄漏监听器
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            code = await WaitForCallbackAsync(listener, state, responseHtml, timeoutCts.Token);
        }
        finally
        {
            listener.Stop();
        }

        var tokens = await ExchangeCodeForTokenAsync(code, redirectUri);
        var client = new GitHubClient(new ProductHeaderValue("BedrockBoot"));
        client.Credentials = new Credentials(tokens.AccessToken);

        var user = await client.User.Current();
        var conf = new ConfigEntity<GithubAccountInfo>(PathsList.GithubAccount);
        conf.Data = new GithubAccountInfo()
        {
            UserImage = user.AvatarUrl,
            // GitHub 允许用户不设置公开昵称，Name 可能为 null，此时回退到登录名
            UserName = string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name,
            UserToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresAt = tokens.ExpiresAt,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
            // 正常情况下 GitHub 会回传实际授予的 scope；个别情况（如 GHES）不回传时按申请值记录
            Scope = tokens.Scope ?? (fullAccess ? "user:email,repo" : "user:email")
        };
        conf.Save();

        return conf.Data;
    }

    /// <summary>
    ///     退出登录时远程吊销令牌，避免本地删掉后凭证仍在服务端有效。
    ///     先吊销 access_token，再删除整个应用授权（这一步才会让 refresh_token 一起失效）。
    ///     纯尽力而为：网络失败只写日志，不阻塞本地清理。
    /// </summary>
    public static async Task RevokeAsync(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken)) return;

        var clientId = BedrockBoot.Core.Keys.GithubClientId;
        var clientSecret = BedrockBoot.Core.Keys.GithubSecret;
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete,
                $"https://api.github.com/applications/{clientId}/token")
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { access_token = accessToken }),
                    Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            request.Headers.TryAddWithoutValidation("User-Agent", "BedrockBoot");

            using var response = await http.SendAsync(request);
            Console.WriteLine($@"吊销 GitHub 令牌：HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"吊销 GitHub 令牌失败（不影响本地退出）：{ex.Message}");
        }

        try
        {
            // 只吊销 access_token 不会让 refresh_token 失效，删掉整条授权才算真正退出
            using var request = new HttpRequestMessage(HttpMethod.Delete,
                $"https://api.github.com/applications/{clientId}/grant");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            request.Headers.TryAddWithoutValidation("User-Agent", "BedrockBoot");

            using var response = await http.SendAsync(request);
            Console.WriteLine($@"删除 GitHub 应用授权：HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"删除 GitHub 应用授权失败（不影响本地退出）：{ex.Message}");
        }
    }

    private static async Task<string> WaitForCallbackAsync(
        HttpListener listener, string state, string successHtml, CancellationToken cancellationToken)
    {
        var buffer = Encoding.UTF8.GetBytes(successHtml);

        while (true)
        {
            var context = await listener.GetContextAsync().WaitAsync(cancellationToken);

            // 浏览器可能发出 favicon 等无关请求，直接忽略
            if (context.Request.Url?.AbsolutePath != CallbackPath)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                continue;
            }

            var query = context.Request.QueryString;
            if (query["state"] != state)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            var code = query["code"];
            if (string.IsNullOrEmpty(code))
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
            return code;
        }
    }

    private async Task<GitHubTokenSet> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        using var http = new System.Net.Http.HttpClient();
        var body = new System.Net.Http.FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("client_id", _clientId),
            new System.Collections.Generic.KeyValuePair<string, string>("client_secret", _clientSecret),
            new System.Collections.Generic.KeyValuePair<string, string>("code", code),
            new System.Collections.Generic.KeyValuePair<string, string>("redirect_uri", redirectUri),
        });

        http.DefaultRequestHeaders.Add("Accept", "application/json");
        var resp = await http.PostAsync("https://github.com/login/oauth/access_token", body);
        var json = await resp.Content.ReadAsStringAsync();

        // 开启短效令牌后响应里会多出 expires_in / refresh_token / refresh_token_expires_in，
        // 必须一并保存，否则 8 小时后只能让用户重新授权
        return GitHubTokenManager.TryParseTokenSet(json)
               ?? throw new InvalidOperationException($"Token exchange failed: {json}");
    }

    private ColorValues GetColors()
    {
        var colors = new ColorValues
        {
            BackgroundBrush = GetColor("BackgroundBrush"),
            PrimaryForegroundBrush = GetColor("PrimaryForegroundBrush"),
            PrimaryDisabledForegroundBrush = GetColor("PrimaryDisabledForegroundBrush"),
            PrimaryDisabled2ForegroundBrush = GetColor("PrimaryDisabled2ForegroundBrush"),
            PrimaryBorderBrush = GetColor("PrimaryBorderBrush"),
            AccentBorderBrush = GetColor("AccentBorderBrush"),
            AccentBackgroundBrush = GetColor("AccentBackgroundBrush"),
            AccentBackgroundOverBrush = GetColor("AccentBackgroundOverBrush")
        };

        return colors;
    }

    private string GetColor(string key)
    {
        try
        {
            if (App.Current?.Resources == null)
                return "#000000";

            if (App.Current.Resources.TryGetValue(key, out var brush) && brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            var darkTheme = App.Current.Resources.TryGetValue("DarkTheme", out var darkThemeDict)
                ? darkThemeDict as ResourceDictionary
                : null;
            var lightTheme = App.Current.Resources.TryGetValue("LightTheme", out var lightThemeDict)
                ? lightThemeDict as ResourceDictionary
                : null;

            var currentTheme = ThemeManager.Instance?.CurrentTheme;
            var targetDict = currentTheme == ThemeVariant.Dark ? darkTheme : lightTheme;

            if (targetDict != null && targetDict.TryGetValue(key, out var themeBrush) &&
                themeBrush is SolidColorBrush themeSolidBrush)
            {
                var color = themeSolidBrush.Color;
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetColor error for key '{key}': {ex.Message}");
        }

        return "#000000";
    }

    private async Task<ColorValues> GetColorsOnUIThreadAsync()
    {
        var tcs = new TaskCompletionSource<ColorValues>();

        if (Dispatcher.UIThread.CheckAccess())
        {
            var colors = GetColors();
            return colors;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var colors = GetColors();
                tcs.SetResult(colors);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }
}

internal class ColorValues
{
    public string BackgroundBrush { get; set; } = "#000000";
    public string PrimaryForegroundBrush { get; set; } = "#000000";
    public string PrimaryDisabledForegroundBrush { get; set; } = "#000000";
    public string PrimaryDisabled2ForegroundBrush { get; set; } = "#000000";
    public string PrimaryBorderBrush { get; set; } = "#000000";
    public string AccentBorderBrush { get; set; } = "#000000";
    public string AccentBackgroundBrush { get; set; } = "#000000";
    public string AccentBackgroundOverBrush { get; set; } = "#000000";
}

public class GithubAccountInfo
{
    [JsonPropertyName("userToken")] public string UserToken { get; set; } = string.Empty;
    [JsonPropertyName("userName")] public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("userImage")] public string UserImage { get; set; } = string.Empty;

    /// <summary>
    ///     刷新凭证。OAuth App 开启短效令牌后由 GitHub 一并下发，一次性使用；
    ///     旧版本保存的长期令牌没有这个字段（为 null），此时不会尝试刷新。
    /// </summary>
    [JsonPropertyName("refreshToken")] public string? RefreshToken { get; set; }

    /// <summary>access_token 的绝对过期时间（UTC）；null 表示长期令牌</summary>
    [JsonPropertyName("expiresAt")] public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>refresh_token 的绝对过期时间（UTC）；null 表示长期有效</summary>
    [JsonPropertyName("refreshTokenExpiresAt")] public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    ///     GitHub 返回的实际授权范围（逗号分隔，例如 <c>repo,user:email</c>）。
    ///     旧版本保存的账户没有这个字段（为 null），此时权限未知。
    /// </summary>
    [JsonPropertyName("scope")] public string? Scope { get; set; }

    /// <summary>是否拿到 repo 权限；null 表示旧账户，无从判断</summary>
    [JsonIgnore]
    public bool? HasRepoScope => Scope is null
        ? null
        : Scope.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Contains("repo", StringComparer.Ordinal);
}