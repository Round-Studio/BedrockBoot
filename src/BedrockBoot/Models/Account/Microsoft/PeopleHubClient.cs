using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;

namespace BedrockBoot.Models.Account.Microsoft;

public class PeopleHubClient
{
    public async Task<XboxAuthEntry.PeopleHubResponse?> GetFriendsListAsync(string authHeader, string xuid)
    {
        Console.WriteLine(@"正在获取好友列表...");

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            httpClient.DefaultRequestHeaders.Add("x-xbl-contract-version", "5");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            string endpoint = string.Format(Constants.PeopleHubEndpoint, xuid);

            var response = await httpClient.GetAsync(endpoint);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($@"获取好友列表失败: {response.StatusCode}");
                Console.WriteLine($@"响应: {responseBody}");
                return null;
            }

            var friendsResponse = JsonSerializer.Deserialize<XboxAuthEntry.PeopleHubResponse>(responseBody);

            Console.WriteLine($@"成功获取好友列表，共 {friendsResponse?.TotalCount ?? 0} 人");
            return friendsResponse;
        }
    }

    public async Task<XboxAuthEntry.XboxProfileResponse?> GetProfileAsync(string authHeader, string xuid)
    {
        Console.WriteLine(@"正在获取Xbox账户信息...");

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            httpClient.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            string endpoint = string.Format(Constants.ProfileEndpoint, xuid);

            var response = await httpClient.GetAsync(endpoint);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($@"获取Xbox账户信息失败: {response.StatusCode}");
                Console.WriteLine($@"响应: {responseBody}");
                return null;
            }

            var profileResponse = JsonSerializer.Deserialize<XboxAuthEntry.XboxProfileResponse>(responseBody);

            if (profileResponse?.ProfileUsers != null && profileResponse.ProfileUsers.Length > 0)
            {
                var user = profileResponse.ProfileUsers[0];
                Console.WriteLine($@"成功获取Xbox账户信息:");
                Console.WriteLine($@"  玩家代号: {user.Settings?.FirstOrDefault(s => s.Id == "Gamertag")?.Value}");
                Console.WriteLine($@"  头像URL: {user.Settings?.FirstOrDefault(s => s.Id == "GameDisplayPicRaw")?.Value}");
            }

            return profileResponse;
        }
    }
}