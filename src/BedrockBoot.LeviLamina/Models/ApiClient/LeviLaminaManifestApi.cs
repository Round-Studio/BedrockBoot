using BedrockBoot.Core.Models;
using BedrockBoot.LeviLamina.Base.Entry.Manifest;
using BedrockBoot.LeviLamina.Global;

namespace BedrockBoot.LeviLamina.Models.ApiClient;

public class LeviLaminaManifestApi : ApiClient<VersionDb>
{
    public static VersionDb? Instance { get; private set; } = null;
    public LeviLaminaManifestApi()
    {
        
    }

    public async Task<VersionDb> GetVersions()
    {
        if (Instance != null) return Instance;

        Instance = (await this.GetAsync(SourceList.LeviLaminaVersion)).Data;
        return Instance;
    }
}