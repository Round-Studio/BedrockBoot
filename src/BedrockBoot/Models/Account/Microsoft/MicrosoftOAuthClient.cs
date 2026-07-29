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
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Base.Entry.Pack.WebServer;
using BedrockBoot.Entity;
using BedrockBoot.Models.Account.Microsoft.Helper;
using BedrockBoot.Models.Global;
using BedrockBoot.Service.WebServer;
using OnePointUI.Avalonia.Style.Core;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Account.Microsoft;

public class MicrosoftOAuthClient
{
    /// <summary>
    /// 获取授权码
    /// </summary>
    /// <returns>授权码和CodeVerifier</returns>
    public async Task<(string? authCode, string? codeVerifier)> GetAuthorizationCodeAsync()
    {
        var (codeVerifier, codeChallenge) = QueryHelpers.GeneratePkceCodes();
        string state = Guid.NewGuid().ToString("N");

        string scope = Uri.EscapeDataString("XboxLive.signin offline_access");
        string authUrl =
            $"https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={Constants.ClientId}&response_type=code&redirect_uri={Uri.EscapeDataString(Constants.RedirectUri)}&response_mode=query&scope={scope}&code_challenge={codeChallenge}&code_challenge_method=S256&state={state}";

        // 打开浏览器
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var tcs = new TaskCompletionSource<(string? authCode, string? codeVerifier)>();
        string? capturedAuthCode = null;
        
        var server = new WebServer($"{Constants.RedirectUri}/");

        // 根路径：接收回调
        server.RegisterRoute("GET", "/", async context =>
        {
            try
            {
                var query = QueryHelpers.ParseQueryString(context.Request.Url.Query);
                string authCode = query["code"];
                capturedAuthCode = authCode;
                
                // 1. 返回登录完成页面（内部包含 Flush 确保流写入）
                await ReturnLoginFinishPage(context);
                
                // 2. 仅通知结果，不要在此处调用 server.Stop()
                tcs.TrySetResult((capturedAuthCode, codeVerifier));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        server.Start();
        
        // 设置超时（5分钟）
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var registration = timeoutCts.Token.Register(() => tcs.TrySetException(new TimeoutException("OAuth 授权超时")));
        
        try
        {
            var result = await tcs.Task;
            await Task.Delay(500); 

            return result;
        }
        finally
        {
            try
            {
                server.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebServer Stop error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 返回登录完成页面
    /// </summary>
    /// <param name="context"></param>
    private async Task ReturnLoginFinishPage(HttpContext context)
    {
        try
        {
            // 读取 HTML 模板
            var responseHtml = await new JsonResourceEntity()
                .ReadTextResourceAsync("avares://BedrockBoot/Assets/Web/LoginFinish.html");

            // 获取颜色值
            var colors = await GetColorsOnUIThreadAsync();
            
            // 替换颜色占位符
            responseHtml = responseHtml
                .Replace("{BackgroundBrush}", colors.BackgroundBrush)
                .Replace("{PrimaryForegroundBrush}", colors.PrimaryForegroundBrush)
                .Replace("{PrimaryDisabledForegroundBrush}", colors.PrimaryDisabledForegroundBrush)
                .Replace("{PrimaryDisabled2ForegroundBrush}", colors.PrimaryDisabled2ForegroundBrush)
                .Replace("{PrimaryBorderBrush}", colors.PrimaryBorderBrush)
                .Replace("{AccentBorderBrush}", colors.AccentBorderBrush)
                .Replace("{AccentBackgroundBrush}", colors.AccentBackgroundBrush)
                .Replace("{AccentBackgroundOverBrush}", colors.AccentBackgroundOverBrush);

            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            await context.Response.OutputStream.FlushAsync(); // 👈 确保缓冲区数据彻底推送到网络层
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ReturnLoginFinishPage error: {ex.Message}");
            
            try
            {
                // 返回简单的错误页面
                string errorHtml = "<html><body><h1>登录完成</h1><p>请返回应用程序继续操作。</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                await context.Response.OutputStream.FlushAsync(); // 👈 兜底错误页面也执行 Flush
                context.Response.Close();
            }
            catch
            {
                // 忽略二次异常
            }
        }
    }

    /// <summary>
    /// 在 UI 线程上获取颜色值
    /// </summary>
    private async Task<ColorValues> GetColorsOnUIThreadAsync()
    {
        var tcs = new TaskCompletionSource<ColorValues>();
        
        if (Dispatcher.UIThread.CheckAccess())
        {
            // 已经在 UI 线程
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

    /// <summary>
    /// 获取所有颜色值（必须在 UI 线程调用）
    /// </summary>
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

    /// <summary>
    /// 获取单个颜色值（必须在 UI 线程调用）
    /// </summary>
    private string GetColor(string key)
    {
        try
        {
            if (App.Current?.Resources == null)
                return "#000000";

            // 直接从 Application.Resources 中查找
            if (App.Current.Resources.TryGetValue(key, out var brush) && brush is SolidColorBrush solidBrush)
            {
                var color = solidBrush.Color;
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        
            // 如果顶层没有，尝试从主题字典中查找
            var darkTheme = App.Current.Resources.TryGetValue("DarkTheme", out var darkThemeDict) 
                ? darkThemeDict as ResourceDictionary 
                : null;
            var lightTheme = App.Current.Resources.TryGetValue("LightTheme", out var lightThemeDict) 
                ? lightThemeDict as ResourceDictionary 
                : null;
        
            // 根据当前主题获取
            var currentTheme = ThemeManager.Instance?.CurrentTheme;
            var targetDict = currentTheme == ThemeVariant.Dark ? darkTheme : lightTheme;
        
            if (targetDict != null && targetDict.TryGetValue(key, out var themeBrush) && themeBrush is SolidColorBrush themeSolidBrush)
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

    /// <summary>
    /// 使用授权码交换访问令牌
    /// </summary>
    public async Task<XboxAuthEntry.OAuthTokenResponse?> ExchangeCodeForTokensAsync(string authCode, string codeVerifier)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("client_id", Constants.ClientId),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authCode),
            new KeyValuePair<string, string>("redirect_uri", Constants.RedirectUri),
            new KeyValuePair<string, string>("code_verifier", codeVerifier)
        };

        var content = new FormUrlEncodedContent(parameters);
        
        try
        {
            var response = await httpClient.PostAsync(Constants.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"ExchangeCodeForTokensAsync failed: {response.StatusCode}, {responseBody}");
                return null;
            }

            var result = JsonSerializer.Deserialize<XboxAuthEntry.OAuthTokenResponse>(responseBody);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ExchangeCodeForTokensAsync error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    public async Task<XboxAuthEntry.OAuthTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        var parameters = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("client_id", Constants.ClientId),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("scope", "XboxLive.signin offline_access")
        };

        var content = new FormUrlEncodedContent(parameters);
        
        try
        {
            var response = await httpClient.PostAsync(Constants.TokenEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"RefreshAccessTokenAsync failed: {response.StatusCode}, {responseBody}");
                return null;
            }

            var result = JsonSerializer.Deserialize<XboxAuthEntry.OAuthTokenResponse>(responseBody);
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RefreshAccessTokenAsync error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 保存认证结果到文件
    /// </summary>
    public static void SaveAuthResult(XboxAuthEntry.AuthResult auth)
    {
        try
        {
            var conf = new ConfigEntity<XboxAuthEntry.AuthResult>(PathsList.MsAccountPath);
            conf.Data = auth;
            conf.Save();
            Debug.WriteLine($"认证结果已保存到: {PathsList.MsAccountPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SaveAuthResult error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从文件加载认证结果
    /// </summary>
    public static XboxAuthEntry.AuthResult? LoadAuthResult()
    {
        try
        {
            if (!File.Exists(PathsList.MsAccountPath))
            {
                Debug.WriteLine("认证文件不存在");
                return null;
            }

            var conf = new ConfigEntity<XboxAuthEntry.AuthResult>(PathsList.MsAccountPath, false);
            var result = conf.Data;
            
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadAuthResult error: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// 颜色值容器
/// </summary>
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