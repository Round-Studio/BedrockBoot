using System.Collections.Generic;

namespace BedrockBoot.Models.Global;

public class SourceList
{
    public static Dictionary<string, string> UpdateDownloadSources { get; set; } = new Dictionary<string, string>()
    {
        { "Github", "{url}" },
        { "加速源 ①", "https://github1.roundstudio.top/{url}" },
        { "llkk.cc", "https://gh.llkk.cc/{url}" },
        { "gh-proxy.top", "https://gh-proxy.top/{url}" },
        { "gh-proxy.net", "https://gh-proxy.net/{url}" }
    };
}