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
    
    public static Func<MsUserConfig?>? GetMsAccountConfig;
    public static Func<MsUserConfig, Task<MsUserConfig>> OnRefreshAccount { get; set; }

    public static void UpdateUseHardwareDecode(bool isUse)
    {
        EasyDownload.UseHardwareDecode = isUse;
    }

    public static void UpdateUseNeoLaunch(bool isUse)
    {
        EasyLauncher.IsUseNeoLaunch = isUse;
    }
}