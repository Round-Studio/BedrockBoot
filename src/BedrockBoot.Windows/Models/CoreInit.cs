using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Core.Global;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Services;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Models;

public class CoreInit
{
    public static async Task Init()
    {
        CoreGlobal.BedrockCore = new BedrockCore
        {
            Options = new CoreOptions
            {
                IsAutoCompleteVC = true,
                IsAutoOpenDevelopment = false,
                IsAutoCompleteGameInput = true,
                IsCheckMD5 = true
            }
        };
        await CoreGlobal.BedrockCore.InitAsync();
    }
    
    public static Func<MsUserConfig?> GetMsAccountConfig;
    public static Func<MsUserConfig, Task<MsUserConfig>>? OnRefreshAccount { get; set; }

    public static void UpdateUseHardwareDecode(bool isUse)
    {
        Console.WriteLine($@"使用硬件解码：{isUse}");
        EasyDownload.UseHardwareDecode = isUse;
    }
}