using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
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
        Round.SDK.Plugin.BedrockBoot.Register.RegisterService.RegisterLaunchingEvent((s =>
        {
            var gameInfo = GameInfoHelper.GetVersionConfig(s);
            var bodyFile = Path.Combine(gameInfo.VersionPath!, gameInfo.BodyFile!);
            var isAdmin = FileCompatibilityChecker.IsRunAsAdminChecked(bodyFile);
            Console.WriteLine($@"当前游戏文件是否需要管理员运行：{isAdmin}");
            if (isAdmin)
            {
                FileCompatibilityChecker.RemoveRunAsAdmin(bodyFile);
                Console.WriteLine(@"已取消文件的管理员权限");
            }
        }));
        
        CoreGlobal.BedrockCore = new BedrockCore
        {
            Options = new CoreOptions
            {
                IsAutoCompleteVC = false,
                IsAutoOpenDevelopment = false,
                IsAutoCompleteGameInput = false,
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