/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using BedrockBoot.Localization;
using BedrockBoot.Standard.Entity.Info;

namespace BedrockBoot.Models.Global;

public class SourceList
{
    public static int MinecraftIconID => 2;
    public static int PackIconID => 1;
    public static int BodyIconID => 0;

    /// <summary>
    ///     获取下载源在当前语言下的显示名；对应语言没有该源的键时回退到源标识本身
    ///     （域名类源的标识就是域名，直接展示即可）。
    /// </summary>
    public static string GetSourceDisplayName(string sourceKey)
    {
        // 域名类源（如 gh.tianpao.top）的标识就是域名，本来就不需要译文；
        // 顺带避免拼出带点号的键名，破坏 Area.Subarea.Name 的约定。
        if (string.IsNullOrEmpty(sourceKey) || sourceKey.Contains('.'))
            return sourceKey;

        return Localizer.Current.Table.TryGetValue("Core.Source." + sourceKey, out var name) ? name : sourceKey;
    }

    public static Dictionary<string, string> UpdateDownloadSources { get; set; } = new()
    {
        { "Github", "{url}" },
        { "Accelerated", "https://github1.roundstudio.top/{url}" },
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
        { "McAppx", "https://data.mcappx.com/v2/bedrock.json" },
        {
            "BedrockBoot",
            "https://raw.giteeusercontent.com/minecraftyjq/bedrock-version-db/raw/main/data/bedrock.json"
        },
        { "BMCBL", "https://api.chlna6666.com/api/v1/bedrock/mcappx" }
    };

    public static Dictionary<string, string> CurseForgeSource { get; set; } = new()
    {
        { "CurseForge", "https://api.curseforge.com/" },
        { "MCIM", "https://mod.mcimirror.top/curseforge/" }
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
    public static string DllModsApi { get; } = "https://pkg.roundstudio.top/index.json";

    public static string LeviLaminaVersionDb =>
        "https://fastly.jsdelivr.net/gh/LiteLDev/levilamina-client-version-db@main/v2/version-db.json";
}