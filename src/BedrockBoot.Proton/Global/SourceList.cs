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

    public static string GameFixUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GameRunningFixKit.tar.gz";
    public static string ProtonXUserUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GDK-Proton-xuser.tar.gz";
    public static string ProtonLauncher =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/Proton-Launch-umu.tar.gz";
}