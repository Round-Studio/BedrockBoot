using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;

namespace BedrockBoot.Models.Account.Microsoft;

public class XboxAuthClient
{
    public async Task<string?> GetXboxUserTokenAsync(string? accessToken)
    {
        using (var httpClient = new HttpClient())
        {
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
            Console.WriteLine($"Xbox User Auth 请求: {jsonRequest}");
            
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(Constants.XboxUserAuthEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Xbox User Auth 响应状态: {response.StatusCode}");
            Console.WriteLine($"Xbox User Auth 响应内容: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Xbox User Auth 失败: {response.StatusCode}");
                return null;
            }

            var result = JsonSerializer.Deserialize<XboxAuthEntry.XboxAuthResponse?>(responseBody);
            return result?.Token;
        }
    }

    public async Task<(string? xstsToken, string? userHash, string? xuid)> GetXstsTokenAsync(string? xboxUserToken)
    {
        using (var httpClient = new HttpClient())
        {
            var request = new Dictionary<string, object>
            {
                ["Properties"] = new Dictionary<string, object>
                {
                    ["SandboxId"] = "RETAIL",
                    ["UserTokens"] = new[] { xboxUserToken }
                },
                ["RelyingParty"] = "http://xboxlive.com",
                ["TokenType"] = "JWT"
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            Console.WriteLine($"XSTS 请求: {jsonRequest}");
            
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(Constants.XstsAuthEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"XSTS 响应状态: {response.StatusCode}");
            Console.WriteLine($"XSTS 响应内容: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    try
                    {
                        var error = JsonSerializer.Deserialize<XboxAuthEntry.XboxErrorResponse?>(responseBody);
                        if (error?.XErr == 2148916233L)
                        {
                            Console.WriteLine(@"错误: 该帐户已被限制访问 Xbox Live 功能");
                        }
                        else if (error?.XErr == 2148916238L)
                        {
                            Console.WriteLine(@"错误: 该帐户尚未通过年龄验证");
                        }
                        else
                        {
                            Console.WriteLine($@"XSTS 错误代码: {error?.XErr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析 XSTS 错误响应失败: {ex.Message}");
                    }
                }

                return (null, null, null);
            }

            var result = JsonSerializer.Deserialize<XboxAuthEntry.XboxAuthResponse?>(responseBody);

            string? userHash = null;
            string? xuid = null;

            if (result?.DisplayClaims?.xui != null && result.DisplayClaims.xui.Length > 0)
            {
                userHash = result.DisplayClaims.xui[0].uhs;
                xuid = result.DisplayClaims.xui[0].xid;
            }

            return (result?.Token, userHash, xuid);
        }
    }
}