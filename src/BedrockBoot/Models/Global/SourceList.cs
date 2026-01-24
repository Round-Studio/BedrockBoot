using System.Collections.Generic;

namespace BedrockBoot.Models.Global;

public class SourceList
{
    public static int MinecraftIconID => 2;
    public static int PackIconID => 1;
    public static int BodyIconID => 0;
    
    public static Dictionary<string, string> UpdateDownloadSources { get; set; } = new Dictionary<string, string>()
    {
        { "Github", "{url}" },
        { "加速源 ①", "https://github1.roundstudio.top/{url}" },
        { "llkk.cc", "https://gh.llkk.cc/{url}" },
        { "gh-proxy.top", "https://gh-proxy.top/{url}" },
        { "gh-proxy.net", "https://gh-proxy.net/{url}" }
    };
    
    public static Dictionary<string, string> VersionDataSources { get; set; } = new Dictionary<string, string>()
    {
        { "McAppx 源", "https://data.mcappx.com/v2/bedrock.json" },
        { "BedrockBoot 源 ①", "https://mcappx.52caecb8.er.aliyun-esa.net" },
        { "BMCBL 源", "https://mcappx.chlna6666.com" }
    };

    public static Dictionary<string, string> CurseForgeSource { get; set; } = new Dictionary<string, string>()
    {
        { "CurseForge 官方源", "https://api.curseforge.com/" },
        /*{ "加速源 ①", "https://blog.zink.dpdns.org/advanced-proxy?url=\"https://api.curseforge.com/{url}\"" },
        { "MCIM 源", "https://mod.mcimirror.top/curseforge/" }*/
    };
}