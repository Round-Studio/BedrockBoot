using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;
using XUserLauncher.Core;

namespace BedrockBoot.Models.Pack.Xbox.Cape;

public class XboxMcAuthClient
{
    public static bool VerboseLogging { get; set; } = true;

    public const string PlayFabRelyingParty = "https://b980a380.minecraft.playfabapi.com/";
    public const string MultiplayerRelyingParty = "https://multiplayer.minecraft.net/";
    public const string RealmsRelyingParty = "https://pocket.realms.minecraft.net/";
    public const string LicensingRelyingParty = "http://licensing.xboxlive.com";
    public const string DefaultXstsRelyingParty = "http://xboxlive.com";

    private const string XboxUserAuthEndpoint = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsAuthEndpoint = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string SisuAuthEndpoint = "https://sisu.xboxlive.com/authorize";
    private const string DeviceAuthEndpoint = "https://device.auth.xboxlive.com/device/authenticate";

    public async Task<string?> GetXboxUserTokenAsync(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            Console.WriteLine(@"中止: accessToken 为空");
            return null;
        }

        using var httpClient = new HttpClient();

        var request = new Dictionary<string, object>
        {
            ["Properties"] = new Dictionary<string, string>
            {
                ["AuthMethod"] = "RPS",
                ["SiteName"] = "user.auth.xboxlive.com",
                ["RpsTicket"] = $"t={accessToken}"
            },
            ["RelyingParty"] = "http://auth.xboxlive.com",
            ["TokenType"] = "JWT"
        };

        var jsonRequest = JsonSerializer.Serialize(request);

        if (VerboseLogging)
        {
            Console.WriteLine($@"请求 URL: {XboxUserAuthEndpoint}");
            Console.WriteLine($@"请求体: {SanitizeJson(jsonRequest)}");
        }

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await httpClient.PostAsync(XboxUserAuthEndpoint, content);
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"请求异常: {ex.GetType().Name}: {ex.Message}");
            return null;
        }

        var statusCode = (int)response.StatusCode;
        Console.WriteLine($@"响应状态: {statusCode} {response.StatusCode}");
        Console.WriteLine($@"响应内容: {(string.IsNullOrWhiteSpace(responseBody) ? "(空)" : responseBody)}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($@"失败: {statusCode} {response.StatusCode}");
            TryParseXboxError(responseBody);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<XboxTokenResponse?>(responseBody);
            if (result?.Token == null)
            {
                Console.WriteLine(@"成功但 Token 为 null，原始响应:");
                Console.WriteLine(responseBody);
            }

            return result?.Token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"反序列化响应失败: {ex.Message}");
            return null;
        }
    }

    public async Task<(string? xstsToken, string? userHash, string? xuid)> GetXstsTokenAsync(
        string? xboxUserToken,
        string? relyingParty = null)
    {
        if (string.IsNullOrWhiteSpace(xboxUserToken))
        {
            Console.WriteLine(@"中止: xboxUserToken 为空");
            return (null, null, null);
        }

        using var httpClient = new HttpClient();

        var request = new Dictionary<string, object>
        {
            ["Properties"] = new Dictionary<string, object>
            {
                ["SandboxId"] = "RETAIL",
                ["UserTokens"] = new[] { xboxUserToken }
            },
            ["RelyingParty"] = relyingParty ?? DefaultXstsRelyingParty,
            ["TokenType"] = "JWT"
        };

        var jsonRequest = JsonSerializer.Serialize(request);

        if (VerboseLogging)
        {
            Console.WriteLine($@"请求 URL: {XstsAuthEndpoint}");
            Console.WriteLine($@"RelyingParty: {relyingParty ?? DefaultXstsRelyingParty}");
            Console.WriteLine($@"请求体: {SanitizeJson(jsonRequest)}");
        }

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await httpClient.PostAsync(XstsAuthEndpoint, content);
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"请求异常: {ex.GetType().Name}: {ex.Message}");
            return (null, null, null);
        }

        var statusCode = (int)response.StatusCode;
        Console.WriteLine($@"响应状态: {statusCode} {response.StatusCode}");
        Console.WriteLine($@"响应内容: {(string.IsNullOrWhiteSpace(responseBody) ? "(空)" : responseBody)}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($@"失败: {statusCode} {response.StatusCode}");
            TryParseXboxError(responseBody);
            return (null, null, null);
        }

        try
        {
            var result = JsonSerializer.Deserialize<XboxTokenResponse?>(responseBody);

            string? userHash = null;
            string? xuid = null;

            if (result?.DisplayClaims?.Xui is { Count: > 0 } xuiList)
            {
                var first = xuiList[0];
                if (first.TryGetValue("uhs", out var uhsElement))
                    userHash = uhsElement.GetString();
                if (first.TryGetValue("xid", out var xidElement))
                    xuid = xidElement.GetString();
            }

            Console.WriteLine($@"成功: uhs={userHash}, xid={xuid}");
            return (result?.Token, userHash, xuid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"反序列化响应失败: {ex.Message}");
            return (null, null, null);
        }
    }

    public async Task<string?> GetDeviceTokenAsync(
        string? deviceId,
        Dictionary<string, string>? proofKey)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            Console.WriteLine(@"中止: deviceId 为空");
            return null;
        }

        if (proofKey == null)
        {
            Console.WriteLine(@"中止: proofKey 为空");
            return null;
        }

        using var httpClient = new HttpClient();

        var request = new Dictionary<string, object>
        {
            ["RelyingParty"] = "http://auth.xboxlive.com",
            ["TokenType"] = "JWT",
            ["Properties"] = new Dictionary<string, object>
            {
                ["AuthMethod"] = "ProofOfPossession",
                ["Id"] = deviceId,
                ["DeviceType"] = "Win32",
                ["Version"] = "10.0.22631",
                ["ProofKey"] = proofKey
            }
        };

        var jsonRequest = JsonSerializer.Serialize(request);

        if (VerboseLogging)
        {
            Console.WriteLine($@"请求 URL: {DeviceAuthEndpoint}");
            Console.WriteLine($@"请求体: {SanitizeJson(jsonRequest)}");
        }

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await httpClient.PostAsync(DeviceAuthEndpoint, content);
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"请求异常: {ex.GetType().Name}: {ex.Message}");
            return null;
        }

        var statusCode = (int)response.StatusCode;
        Console.WriteLine($@"响应状态: {statusCode} {response.StatusCode}");
        Console.WriteLine($@"响应内容: {(string.IsNullOrWhiteSpace(responseBody) ? "(空)" : responseBody)}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($@"失败: {statusCode} {response.StatusCode}");
            TryParseXboxError(responseBody);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<XboxTokenResponse?>(responseBody);
            return result?.Token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"反序列化响应失败: {ex.Message}");
            return null;
        }
    }

    public async Task<(string? sisuToken, string? userHash, string? xuid)> GetSisuTokenAsync(
        string? msaAccessToken,
        string? deviceToken,
        Dictionary<string, string>? proofKey,
        string relyingParty)
    {
        if (string.IsNullOrWhiteSpace(msaAccessToken))
        {
            Console.WriteLine(@"中止: msaAccessToken 为空");
            return (null, null, null);
        }

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            Console.WriteLine(@"中止: deviceToken 为空");
            return (null, null, null);
        }

        if (proofKey == null)
        {
            Console.WriteLine(@"中止: proofKey 为空");
            return (null, null, null);
        }

        using var httpClient = new HttpClient();

        var request = new Dictionary<string, object>
        {
            ["AccessToken"] = $"t={msaAccessToken}",
            ["AppId"] = "0000000048183522",
            ["deviceToken"] = deviceToken,
            ["Sandbox"] = "RETAIL",
            ["UseModernGamertag"] = true,
            ["SiteName"] = "user.auth.xboxlive.com",
            ["RelyingParty"] = relyingParty,
            ["OfferTermsAcceptance"] = true,
            ["AcceptOffers"] = true,
            ["ProofKey"] = proofKey
        };

        var jsonRequest = JsonSerializer.Serialize(request);

        if (VerboseLogging)
        {
            Console.WriteLine($@"请求 URL: {SisuAuthEndpoint}");
            Console.WriteLine($@"RelyingParty: {relyingParty}");
            Console.WriteLine($@"请求体: {SanitizeJson(jsonRequest)}");
        }

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await httpClient.PostAsync(SisuAuthEndpoint, content);
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"请求异常: {ex.GetType().Name}: {ex.Message}");
            return (null, null, null);
        }

        var statusCode = (int)response.StatusCode;
        Console.WriteLine($@"响应状态: {statusCode} {response.StatusCode}");
        Console.WriteLine($@"响应内容: {(string.IsNullOrWhiteSpace(responseBody) ? "(空)" : responseBody)}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($@"失败: {statusCode} {response.StatusCode}");
            TryParseXboxError(responseBody);
            return (null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(responseBody);

            string? sisuToken = null;
            string? userHash = null;
            string? xuid = null;

            if (doc.RootElement.TryGetProperty("AuthorizationToken", out var authToken) &&
                authToken.TryGetProperty("Token", out var tokenValue))
            {
                sisuToken = tokenValue.GetString();
            }

            if (doc.RootElement.TryGetProperty("AuthorizationToken", out var authToken2) &&
                authToken2.TryGetProperty("DisplayClaims", out var displayClaims) &&
                displayClaims.TryGetProperty("xui", out var xui) &&
                xui.ValueKind == JsonValueKind.Array &&
                xui.GetArrayLength() > 0)
            {
                var first = xui[0];
                if (first.TryGetProperty("uhs", out var uhs))
                    userHash = uhs.GetString();
                if (first.TryGetProperty("xid", out var xid))
                    xuid = xid.GetString();
            }

            Console.WriteLine($@"成功: uhs={userHash}, xid={xuid}");
            return (sisuToken, userHash, xuid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"反序列化响应失败: {ex.Message}");
            return (null, null, null);
        }
    }

    private static void TryParseXboxError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return;

        try
        {
            var error = JsonSerializer.Deserialize<XboxAuthEntry.XboxErrorResponse?>(responseBody);
            if (error == null) return;

            Console.WriteLine($@"XErr: {error.XErr}");

            if (error.XErr == 2148916233L)
                Console.WriteLine(@"该帐户没有 Xbox 账户");
            else if (error.XErr == 2148916238L)
                Console.WriteLine(@"该帐户尚未通过年龄验证");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"解析错误响应失败: {ex.Message}");
        }
    }

    private static string SanitizeJson(string json)
    {
        json = Regex.Replace(json, @"""RpsTicket"":""[^""]*""", @"""RpsTicket"":""***""");
        json = Regex.Replace(json, @"""UserTokens"":\[[^\]]*\]", @"""UserTokens"":[""***""]");
        json = Regex.Replace(json, @"""AccessToken"":""[^""]*""", @"""AccessToken"":""***""");
        json = Regex.Replace(json, @"""deviceToken"":""[^""]*""", @"""deviceToken"":""***""");
        json = Regex.Replace(json, @"""ProofKey"":\{[^}]*\}", @"""ProofKey"":{***}");
        return json;
    }
}