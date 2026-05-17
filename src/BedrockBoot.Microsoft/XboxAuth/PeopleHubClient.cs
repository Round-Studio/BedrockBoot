using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Account.Microsoft;

namespace BedrockBoot.Microsoft.XboxAuth
{
    public class PeopleHubClient
    {
        public async Task GetFriendsListAsync(string authHeader, string xuid)
        {
            Console.WriteLine();
            Console.WriteLine("=== 正在获取好友列表 ===");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                httpClient.DefaultRequestHeaders.Add("x-xbl-contract-version", "5");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                string endpoint = string.Format(Constants.PeopleHubEndpoint, xuid);

                var response = await httpClient.GetAsync(endpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"获取好友列表失败: {response.StatusCode}");
                    Console.WriteLine($"响应: {responseBody}");
                    return;
                }

                var friendsResponse = JsonSerializer.Deserialize<XboxAuthEntry.PeopleHubResponse>(responseBody);

                Console.WriteLine($"\n✅ 成功获取好友列表，共 {friendsResponse?.TotalCount ?? 0} 人");
                Console.WriteLine("=======================================");

                if (friendsResponse?.People != null)
                {
                    int index = 1;
                    foreach (var person in friendsResponse.People)
                    {
                        Console.WriteLine($"{index}. 玩家代号: {person.Gamertag}");
                        Console.WriteLine($"   XUID: {person.Xuid}");
                        Console.WriteLine($"   真实姓名: {(string.IsNullOrEmpty(person.RealName) ? "未设置" : person.RealName)}");
                        Console.WriteLine($"   显示名称: {person.DisplayName}");
                        Console.WriteLine($"   是否有Xbox Live档案: {(person.HasXboxLiveProfile ? "是" : "否")}");
                        Console.WriteLine($"   是否特别关注: {(person.IsFavorite ? "是" : "否")}");
                        Console.WriteLine($"   是否双向好友: {(person.IsFollowingCaller ? "是" : "否")}");
                        Console.WriteLine($"   关注者数量: {person.FollowerCount}");
                        Console.WriteLine($"   关注数量: {person.FollowingCount}");

                        if (person.Presence?.State != null)
                        {
                            Console.WriteLine($"   在线状态: {person.Presence.State}");
                        }
                        else
                        {
                            Console.WriteLine($"   在线状态: 离线");
                        }

                        Console.WriteLine("---------------------------------------");
                        index++;

                        if (index > 20)
                        {
                            Console.WriteLine($"... 还有 {friendsResponse.People.Length - 20} 位好友未显示");
                            break;
                        }
                    }
                }

                File.WriteAllText(Constants.FriendsResponseFile, responseBody);
                Console.WriteLine($"\n完整响应已保存到 {Constants.FriendsResponseFile}");
            }
        }
    }
}

