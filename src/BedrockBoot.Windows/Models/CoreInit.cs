using BedrockBoot.Core.Global;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
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
}