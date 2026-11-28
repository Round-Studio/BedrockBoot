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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Models.Account.Github;

public static class GitHubApiClient
{
    private const string ProductName = "BedrockBoot";

    /// <summary>
    ///     只读本地保存的令牌，不续期。需要保证令牌可用时用 <see cref="GetUserTokenAsync" />；
    ///     同步版本只留给不便 await 的场景（如下载器取请求头）。
    /// </summary>
    public static string? GetUserToken() => GitHubTokenManager.ReadAccount()?.UserToken;

    /// <summary>取得可用的令牌，快过期时自动续期</summary>
    public static Task<string?> GetUserTokenAsync() => GitHubTokenManager.GetValidTokenAsync();

    public static GitHubClient CreateClient(string? token)
    {
        var client = new GitHubClient(new ProductHeaderValue(ProductName));
        if (!string.IsNullOrEmpty(token))
            client.Credentials = new Credentials(token);

        return client;
    }

    /// <summary>创建已带上可用令牌的客户端；需要 401 重试时请用 <see cref="ExecuteAsync{T}" /></summary>
    public static async Task<GitHubClient> CreateClientAsync()
    {
        return CreateClient(await GitHubTokenManager.GetValidTokenAsync());
    }

    /// <summary>
    ///     执行一次 GitHub API 调用，令牌过期时自动续期；服务端返回 401（令牌被提前撤销等）
    ///     时强制刷新一次并重试，第二次仍失败则把异常抛给调用方。
    ///     <para>
    ///     本地令牌取不到可用值（过期且续期失败、被撤销等）时按匿名继续执行：
    ///     检查更新、贡献者列表这类公开端点不依赖登录，不应被本机凭证问题拦住；
    ///     私有数据会让请求本身报 404/403，由调用方按原有错误路径提示。
    ///     </para>
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(Func<GitHubClient, Task<T>> action)
    {
        string? token = null;
        try
        {
            token = await GitHubTokenManager.GetValidTokenAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"GitHub 令牌不可用，本次请求按匿名执行：{ex.Message}");
        }

        var client = CreateClient(token);

        try
        {
            return await action(client);
        }
        catch (Exception ex) when (IsUnauthorized(ex))
        {
            Console.WriteLine(@"GitHub 请求返回 401，尝试刷新令牌后重试一次");

            // 刷新失败会抛 GitHubReauthRequiredException，由界面提示重新登录
            var refreshed = await GitHubTokenManager.GetValidTokenAsync(true, token);
            if (string.IsNullOrEmpty(refreshed) || refreshed == token)
                throw;

            client.Credentials = new Credentials(refreshed);
            return await action(client);
        }
    }

    public static async Task ExecuteAsync(Func<GitHubClient, Task> action)
    {
        await ExecuteAsync(async client =>
        {
            await action(client);
            return true;
        });
    }

    /// <summary>
    ///     把 Actions 产物的 API 地址换成可直接下载的签名地址。
    ///     <para>
    ///     <c>/actions/artifacts/{id}/zip</c> 只会返回 302，跳转目标是带 SAS 签名的 blob 地址：
    ///     它只认签名，请求里一旦同时带上 GitHub 的 Authorization 头，Azure 会按
    ///     "认证失败" 直接回 403（表现为下载失败 403 Forbidden）。
    ///     所以这里手动取一次 Location，调用方拿到签名地址后用<b>不带鉴权头</b>的下载器拉取。
    ///     </para>
    /// </summary>
    public static async Task<string> ResolveArtifactDownloadUrlAsync(string archiveUrl,
        CancellationToken cancellationToken = default)
    {
        var token = await GitHubTokenManager.GetValidTokenAsync();
        var result = await RequestArtifactRedirectAsync(archiveUrl, token, cancellationToken);

        // 令牌被提前撤销时换一个再试一次；签名地址本身与令牌无关
        if (result.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            token = await GitHubTokenManager.GetValidTokenAsync(true, token);
            if (!string.IsNullOrEmpty(token))
                result = await RequestArtifactRedirectAsync(archiveUrl, token, cancellationToken);
        }

        if (!string.IsNullOrEmpty(result.Location))
        {
            // 先探一个字节：签名地址本身有问题（过期、被代理改写）时，在这里就能拿到
            // Azure 的错误说明，而不是让下载器抛一个没有上下文的裸 403
            var (ok, status, body) = await ProbeSignedUrlAsync(result.Location, cancellationToken);
            if (!ok)
                throw new InvalidOperationException($"更新包签名地址无法访问（HTTP {(int)status}）：{body}");

            return result.Location;
        }

        // 令牌没带 repo 权限时，GitHub 会用 403 要求 "actions" scope；原始文案对用户没有
        // 可操作性，换成「重新登录并同意新权限」的说明
        if (result.StatusCode == HttpStatusCode.Forbidden &&
            result.Body.Contains("actions scope", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(I18nManager.Instance["Task.Update.Error.ArtifactScope"]);

        // 没有跳转说明 API 直接拒绝了：把 GitHub 的错误说明带出来，别再报一个裸 403
        throw new InvalidOperationException(
            $"无法获取更新包下载地址（HTTP {(int)result.StatusCode}）：{result.Body}");
    }

    /// <summary>用 1 字节的 Range 请求验证签名地址可用（同时验证支持分段下载）</summary>
    private static async Task<(bool Ok, HttpStatusCode StatusCode, string Body)> ProbeSignedUrlAsync(
        string url, CancellationToken cancellationToken)
    {
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ProductName);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.IsSuccessStatusCode) return (true, response.StatusCode, string.Empty);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 300) body = body[..300];
        return (false, response.StatusCode, body.Trim());
    }

    private static async Task<ArtifactRedirectResult> RequestArtifactRedirectAsync(string url, string? token,
        CancellationToken cancellationToken)
    {
        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseProxy = false
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ProductName);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var location = response.Headers.Location?.ToString();
        if (!string.IsNullOrEmpty(location) && !Uri.IsWellFormedUriString(location, UriKind.Absolute))
            location = new Uri(new Uri(url), location).ToString();

        var body = string.Empty;
        if (string.IsNullOrEmpty(location))
        {
            // 只有失败时才读正文（GitHub 的说明都在里面），截断避免把整页 HTML 读进内存
            body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 300) body = body[..300];
        }

        return new ArtifactRedirectResult(response.StatusCode,
            string.IsNullOrEmpty(location) ? null : location, body.Trim());
    }

    private readonly record struct ArtifactRedirectResult(HttpStatusCode StatusCode, string? Location, string Body);

    private static bool IsUnauthorized(Exception exception) =>
        exception is ApiException { StatusCode: HttpStatusCode.Unauthorized };
}
