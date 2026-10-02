using BedrockBoot.Models.Global;
using BedrockBoot.Standard.Interface.Platform;
using BedrockLauncher.Core;

namespace BedrockBoot;

public class LinuxCoreInit : ICoreInit
{
    public async Task InitializeAsync()
    {
        CoreGlobal.BedrockCore = new BedrockCore {};
    }

    public void UpdateUseHardwareDecode(bool isUse)
    {
        Console.WriteLine($@"使用硬件解码：{isUse}");
        IsUseHardwareDecode = isUse;
    }

    public bool IsUseHardwareDecode { get; set; }
}