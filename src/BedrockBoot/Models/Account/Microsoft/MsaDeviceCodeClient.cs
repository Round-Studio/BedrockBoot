using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Account.Microsoft;

public class MsaDeviceCodeClient
{
    readonly HttpClient _http = new();

    public class TokenData
    {
        public string? Code { get; set; }
        public string? CodeVerifier { get; set; }
        public string? RedirectUri { get; set; }
        public string? ClientId { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int? ExpiresIn { get; set; }
        public string? SavedAt { get; set; }
    }

    public record DeviceCodeResponse(
        string DeviceCode,
        string UserCode,
        string VerificationUri,
        int Interval,
        int ExpiresIn
    );

    public record TokenResponse(
        string? RefreshToken,
        string? AccessToken,
        int? ExpiresIn,
        string? Error,
        string? ErrorDescription
    );

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };
    
    public Action<string,string>? OnLoginCallback { get; set; }

    public TokenData? Refresh(string refreshToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = Constants.MsaClientId,
            ["scope"] = Constants.MsaScope,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };
        try
        {
            var resp = _http.PostAsync(Constants.MsaTokenUrl, new FormUrlEncodedContent(fields)).Result;
            var json = resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>().Result;
            if (json != null && json.TryGetValue("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String
                && json.TryGetValue("access_token", out var at) && at.ValueKind == JsonValueKind.String)
            {
                return new TokenData
                {
                    RefreshToken = rt.GetString()!,
                    AccessToken = at.GetString()!,
                    ExpiresIn = json.TryGetValue("expires_in", out var ei) ? ei.GetInt32() : null,
                    SavedAt = DateTimeOffset.UtcNow.ToString("o"),
                };
            }
        }
        catch { }
        return null;
    }

    public DeviceCodeResponse? RequestDeviceCode()
    {
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = Constants.MsaClientId,
            ["scope"] = Constants.MsaScope,
            ["response_type"] = "device_code",
        };
        try
        {
            var resp = _http.PostAsync(Constants.MsaConnectUrl, new FormUrlEncodedContent(fields)).Result;
            var json = resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>().Result;
            if (json == null || !json.TryGetValue("device_code", out var dc)) return null;
            return new DeviceCodeResponse(
                dc.GetString()!,
                json["user_code"].GetString()!,
                json.TryGetValue("verification_uri", out var vu) ? vu.GetString()! : "https://www.microsoft.com/link",
                json.TryGetValue("interval", out var iv) ? iv.GetInt32() : 5,
                json.TryGetValue("expires_in", out var ei) ? ei.GetInt32() : 900
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"RequestDeviceCode failed: {ex.Message}");
            return null;
        }
    }

    public TokenResponse? PollDeviceCode(string deviceCode)
    {
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = Constants.MsaClientId,
            ["grant_type"] = "device_code",
            ["device_code"] = deviceCode,
        };
        try
        {
            var resp = _http.PostAsync(Constants.MsaTokenUrl, new FormUrlEncodedContent(fields)).Result;
            var json = resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>().Result;
            if (json == null) return null;
            return new TokenResponse(
                json.TryGetValue("refresh_token", out var rt) ? rt.GetString() : null,
                json.TryGetValue("access_token", out var at) ? at.GetString() : null,
                json.TryGetValue("expires_in", out var ei) ? ei.GetInt32() : null,
                json.TryGetValue("error", out var er) ? er.GetString() : null,
                json.TryGetValue("error_description", out var ed) ? ed.GetString() : null
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"PollDeviceCode failed: {ex.Message}");
            return null;
        }
    }

    public bool RunDeviceCodeFlow()
    {
        var dc = RequestDeviceCode();
        if (dc == null)
        {
            Console.WriteLine(@"Failed to get device code");
            return false;
        }

        Console.WriteLine($@"Microsoft sign-in -> {dc.VerificationUri}");
        Console.WriteLine($@"Code: {dc.UserCode}");

        var deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + dc.ExpiresIn;
        var interval = Math.Max(dc.Interval, 1);

        while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < deadline)
        {
            Thread.Sleep(interval * 1000);
            var tr = PollDeviceCode(dc.DeviceCode);
            if (tr == null) continue;
            if (tr.Error == "authorization_pending") continue;
            if (tr.Error == "slow_down") { interval += 5; continue; }
            if (tr.Error != null)
            {
                Console.WriteLine($@"Sign-in failed: {tr.ErrorDescription ?? tr.Error}");
                return false;
            }
            if (tr.RefreshToken != null)
            {
                Console.WriteLine(@"Microsoft account linked");
                return true;
            }
        }
        Console.WriteLine(@"Sign-in timed out");
        return false;
    }

    public async Task<(bool success, TokenData? tokenData, string? userCode, string? verificationUri)> RunDeviceCodeFlowAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("正在请求设备代码...");
        var dc = RequestDeviceCode();
        if (dc == null)
        {
            progress?.Report("请求设备代码失败");
            return (false, null, null, null);
        }

        progress?.Report($"请在浏览器中打开: {dc.VerificationUri}");
        progress?.Report($"输入代码: {dc.UserCode}");

        OnLoginCallback?.Invoke(dc.VerificationUri, dc.UserCode);

        var deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + dc.ExpiresIn;
        var interval = Math.Max(dc.Interval, 5);

        while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(interval * 1000, cancellationToken);
            var tr = PollDeviceCode(dc.DeviceCode);
            if (tr == null) continue;
            if (tr.Error == "authorization_pending")
            {
                progress?.Report($"等待用户授权... (剩余 {Math.Max(0, deadline - DateTimeOffset.UtcNow.ToUnixTimeSeconds())} 秒)");
                continue;
            }
            if (tr.Error == "slow_down")
            {
                interval += 5;
                progress?.Report("请求过于频繁，减慢轮询速度");
                continue;
            }
            if (tr.Error != null)
            {
                progress?.Report($"登录失败: {tr.ErrorDescription ?? tr.Error}");
                return (false, null, dc.UserCode, dc.VerificationUri);
            }
            if (tr.RefreshToken != null)
            {
                var tokenData = new TokenData
                {
                    AccessToken = tr.AccessToken,
                    RefreshToken = tr.RefreshToken,
                    ExpiresIn = tr.ExpiresIn,
                    SavedAt = DateTimeOffset.UtcNow.ToString("o"),
                };
                progress?.Report("登录成功！");
                return (true, tokenData, dc.UserCode, dc.VerificationUri);
            }
        }

        progress?.Report("登录超时");
        return (false, null, dc.UserCode, dc.VerificationUri);
    }
    
    public async Task<TokenData?> RefreshTokenAsync(string refreshToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["client_id"] = Constants.MsaClientId,
            ["scope"] = Constants.MsaScope,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };
        try
        {
            var resp = await _http.PostAsync(Constants.MsaTokenUrl, new FormUrlEncodedContent(fields));
            var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
            if (json != null && json.TryGetValue("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String
                && json.TryGetValue("access_token", out var at) && at.ValueKind == JsonValueKind.String)
            {
                return new TokenData
                {
                    RefreshToken = rt.GetString()!,
                    AccessToken = at.GetString()!,
                    ExpiresIn = json.TryGetValue("expires_in", out var ei) ? ei.GetInt32() : null,
                    SavedAt = DateTimeOffset.UtcNow.ToString("o"),
                };
            }
        }
        catch { }
        return null;
    }
}