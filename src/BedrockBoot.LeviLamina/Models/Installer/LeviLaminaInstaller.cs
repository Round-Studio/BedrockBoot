using BedrockBoot.Base.Entry.Game;
using BedrockBoot.LeviLamina.Models.ApiClient;

namespace BedrockBoot.LeviLamina.Models.Installer;

public class LeviLaminaInstaller
{
    public VersionConfig VersionInfo { get; private set; }
    public LeviLaminaInstaller(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig;
    }

    public async Task<List<string>> GetVersions()
    {
        var lmaDb = await new LeviLaminaManifestApi().GetVersions();
        var result = new List<string>();
        lmaDb.Versions.Keys.ToList().ForEach(x => 
        {
            if (VersionInfo.Info.Version.Replace(".", "").StartsWith(x))
            {
                result = lmaDb.Versions[x];
            }
        });

        if (result.Count <= 0) throw new NullReferenceException("这个版本不适用于 LeviLamina 喵");

        return result;
    } // 获取符合该版本的 LeviLamina 列表
}