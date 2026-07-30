using BedrockBoot.Models.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Models;

public class CoreInit
{
    public static async Task Init()
    {
        CoreGlobal.BedrockCore = new BedrockCore {};
    }

    public static void SetMsAccount(string accessToken,string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public static string AccessToken { get; set; }
    public static string RefreshToken { get; set; }

    public static void UpdateUseHardwareDecode(bool isUse)
    {
        EasyDownload.UseHardwareDecode = isUse;
    }

    public static void UpdateUseNeoLaunch(bool isUse)
    {
        EasyLauncher.IsUseNeoLaunch = isUse;
    }
}