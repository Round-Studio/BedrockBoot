using BedrockBoot.Proton.Enum;
using Octokit;

namespace BedrockBoot.Proton.Global;

public class SourceList
{
    private static GitHubClient _client = new GitHubClient(new ProductHeaderValue("BedrockBoot.Linux"));
    public static Dictionary<ProtonSource, Task<IReadOnlyList<Release>>> ProtonRepository { get; } = new()
    {
        {
            ProtonSource.WeatherOS,
            _client.Repository.Release.GetAll("Weather-OS", "GDK-Proton")
        },
        {
            ProtonSource.LukasPAH,
            _client.Repository.Release.GetAll("LukasPAH", "GDK-Proton-Custom")
        }
    };
}