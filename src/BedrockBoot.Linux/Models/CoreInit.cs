using BedrockBoot.Base.Entry.Account.Microsoft;
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
    
    public static MsUserConfig? MsUserConfig { get;private set; }
    public static Action<MsUserConfig>? OnRefreshAccount { get; set; }

    public static void SetMsAccount(MsUserConfig config)
    {
        MsUserConfig = config;
    }

    public static void UpdateUseHardwareDecode(bool isUse)
    {
        EasyDownload.UseHardwareDecode = isUse;
    }

    public static void UpdateUseNeoLaunch(bool isUse)
    {
        EasyLauncher.IsUseNeoLaunch = isUse;
    }
}