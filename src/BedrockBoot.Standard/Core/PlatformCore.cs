using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Standard.Interface.Platform;

namespace BedrockBoot.Standard.Core;

public class PlatformCore
{
    public static Func<MsUserConfig?>? GetMsAccountConfig;
    public static Func<MsUserConfig, Task<MsUserConfig>>? OnRefreshAccount { get; set; }
    public static ICoreInit? CoreInit { get; private set; } = null;
    public static async Task InstallAsync(ICoreInit coreInit)
    {
        CoreInit = coreInit;

        await CoreInit.InitializeAsync();
    }
}