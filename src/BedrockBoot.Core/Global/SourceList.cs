using System.Collections.Generic;
using System.IO;
using BedrockBoot.Base.Entry.Info;

namespace BedrockBoot.Models.Global;

public class SourceList
{
    public static int MinecraftIconID => 2;
    public static int PackIconID => 1;
    public static int BodyIconID => 0;

    public static Dictionary<string, string> UpdateDownloadSources { get; set; } = new()
    {
        { "Github", "{url}" },
        { "加速源 ①", "https://github1.roundstudio.top/{url}" },
        { "gh.tianpao.top", "https://gh.tianpao.top/{url}" },
        { "gh.tiouo.cc", "https://gh.tiouo.cc/{route}" },
        { "gh-proxy.com", "https://gh-proxy.com/{url}" },
        { "gh-proxy.net", "https://gh-proxy.net/{url}" },
        { "gh-proxy.org", "https://gh-proxy.org/{url}" },
        { "gitproxy.click", "https://gitproxy.click/{url}" }
        /*{ "", "{route}" }*/
    };

    public static Dictionary<string, string> VersionDataSources { get; set; } = new()
    {
        { "McAppx 源", "https://data.mcappx.com/v2/bedrock.json" },
        { "BedrockBoot 源 ①", "https://raw.giteeusercontent.com/minecraftyjq/bedrock-version-db/raw/main/data/bedrock.json" },
        { "BMCBL 源 ①", "https://api.chlna6666.com/api/v1/bedrock/mcappx" }
    };

    public static Dictionary<string, string> CurseForgeSource { get; set; } = new()
    {
        { "CurseForge 官方源", "https://api.curseforge.com/" },
        { "MCIM 源", "https://mod.mcimirror.top/curseforge/" }
    };

    public static List<GameDownloadUrlInfo> GameFileDownloadSource { get; set; } = new()
    {
        new GameDownloadUrlInfo
        {
            Host = "assets1.xboxlive.cn",
            Url = "http://assets1.xboxlive.cn{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "assets2.xboxlive.cn",
            Url = "http://assets2.xboxlive.cn{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "assets1.xboxlive.com",
            Url = "http://assets1.xboxlive.com{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "assets2.xboxlive.com",
            Url = "http://assets2.xboxlive.com/{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "xvcf1.xboxlive.com",
            Url = "http://xvcf1.xboxlive.com{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "xvcf2.xboxlive.com",
            Url = "http://xvcf2.xboxlive.com/{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "d1.xboxlive.cn",
            Url = "http://d1.xboxlive.cn/{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "d2.xboxlive.cn",
            Url = "http://d2.xboxlive.cn/{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "d1.xboxlive.com",
            Url = "http://d1.xboxlive.com/{router}"
        },
        new GameDownloadUrlInfo
        {
            Host = "d2.xboxlive.com",
            Url = "http://d2.xboxlive.com/{router}"
        }
    };

    public static string MojangHost { get; } = "https://launchercontent.mojang.com/v2";
    public static string NewsUrl { get; } = MojangHost + "/bedrockPatchNotes.json";
    public static string VC20152022Url { get; } = "https://aka.ms/vc14/vc_redist.x64.exe";
    public static string MarketApiHost { get; } = "https://market-api.roundstudio.top";
    public static string PluginApi { get; } = $"{MarketApiHost}/api/plugins";
}