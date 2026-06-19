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

    public static void UpdateUseHardwareDecode(bool isUse)
    {
        EasyDownload.UseHardwareDecode = isUse;
    }
}