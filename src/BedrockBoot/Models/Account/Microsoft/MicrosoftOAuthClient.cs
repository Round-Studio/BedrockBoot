using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Entity;
using BedrockBoot.Models.Account.Microsoft.Helper;
using BedrockBoot.Service.WebServer;

namespace BedrockBoot.Models.Account.Microsoft;

public class MicrosoftOAuthClient
{
    public async Task<(string? authCode, string? codeVerifier)> GetAuthorizationCodeAsync()
    {
        var (codeVerifier, codeChallenge) = QueryHelpers.GeneratePkceCodes();
        string state = Guid.NewGuid().ToString("N");

        string scope = Uri.EscapeDataString("XboxLive.signin offline_access");
        string authUrl =
            $"https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={Constants.ClientId}&response_type=code&redirect_uri={Uri.EscapeDataString(Constants.RedirectUri)}&response_mode=query&scope={scope}&code_challenge={codeChallenge}&code_challenge_method=S256&state={state}";

        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        // 使用 TaskCompletionSource 来等待回调结果
        var tcs = new TaskCompletionSource<(string? authCode, string? codeVerifier)>();
        string? capturedAuthCode = null;
        
        var server = new WebServer($"{Constants.RedirectUri}/");

        // 根路径：接收回调，验证参数，然后重定向到 /loginFinish
        server.RegisterRoute("GET", "/", async context =>
        {
            try
            {
                var query = QueryHelpers.ParseQueryString(context.Request.Url.Query);
                string receivedState = query["state"];
                
                if (receivedState != state)
                {
                    // state 不匹配，重定向到错误页面
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", "/loginFinish?error=invalid_state");
                    context.Response.Close();
                    return;
                }

                string error = query["error"];
                if (!string.IsNullOrEmpty(error))
                {
                    // 有错误，重定向到错误页面
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", $"/loginFinish?error={Uri.EscapeDataString(error)}");
                    context.Response.Close();
                    return;
                }

                string authCode = query["code"];
                if (string.IsNullOrEmpty(authCode))
                {
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", "/loginFinish?error=no_code");
                    context.Response.Close();
                    return;
                }

                // 保存 authCode
                capturedAuthCode = authCode;
                
                // 重定向到登录完成页面
                context.Response.StatusCode = 302;
                context.Response.Headers.Add("Location", "/loginFinish");
                context.Response.Close();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                server.Stop();
            }
        });

        // /loginFinish 路径：返回 HTML 页面并完成 Task
        server.RegisterRoute("GET", "/loginFinish", async context =>
        {
            try
            {
                var query = QueryHelpers.ParseQueryString(context.Request.Url.Query);
                string error = query["error"];

                string responseHtml;
                
                if (!string.IsNullOrEmpty(error))
                {
                    // 加载错误页面
                    responseHtml = await new JsonResourceEntity()
                        .ReadTextResourceAsync("avares://BedrockBoot/Assets/Web/LoginError.html");
                    // 或者你可以使用自定义的错误消息替换
                    responseHtml = responseHtml.Replace("{{error_message}}", error);
                    
                    byte[] errorBuffer = Encoding.UTF8.GetBytes(responseHtml);
                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = errorBuffer.Length;
                    await context.Response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    context.Response.Close();
                    
                    tcs.SetResult((null, null));
                }
                else
                {
                    // 加载成功页面
                    responseHtml = await new JsonResourceEntity()
                        .ReadTextResourceAsync("avares://BedrockBoot/Assets/Web/LoginFinish.html");
                    
                    byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    context.Response.Close();
                    
                    tcs.SetResult((capturedAuthCode, codeVerifier));
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                server.Stop();
            }
        });

        server.Start();
        
        // 等待回调完成
        return await tcs.Task;
    }

    public async Task<XboxAuthEntry.OAuthTokenResponse?> ExchangeCodeForTokensAsync(string authCode, string codeVerifier)
    {
        using (var httpClient = new HttpClient())
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", Constants.ClientId),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("redirect_uri", Constants.RedirectUri),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync(Constants.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer.Deserialize<XboxAuthEntry.OAuthTokenResponse?>(responseBody);
        }
    }

    public async Task<XboxAuthEntry.OAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        using (var httpClient = new HttpClient())
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", Constants.ClientId),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("scope", "XboxLive.signin offline_access")
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync(Constants.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer.Deserialize<XboxAuthEntry.OAuthTokenResponse?>(responseBody);
        }
    }

    public void SaveAuthResult(XboxAuthEntry.AuthResult auth)
    {
        var json = JsonSerializer.Serialize(auth, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Constants.AuthResultFile, json);
    }

    public XboxAuthEntry.AuthResult? LoadAuthResult()
    {
        try
        {
            if (!File.Exists(Constants.AuthResultFile))
                return null;

            var json = File.ReadAllText(Constants.AuthResultFile);
            return JsonSerializer.Deserialize<XboxAuthEntry.AuthResult>(json);
        }
        catch
        {
            return null;
        }
    }
}