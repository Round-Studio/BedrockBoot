using System.Collections.Generic;
using BedrockBoot.Models.Enum;

namespace BedrockBoot.Models.Public;

public class Source
{
    public static Dictionary<ListSourceEnum, string> ListSources = new Dictionary<ListSourceEnum, string>()
    {
        [ListSourceEnum.GitCode] =
            "https://raw.gitcode.com/gcw_lJgzYtGB/-MineCraft-Bedrock-Download-SU/raw/main/bedrock.json",
        [ListSourceEnum.McAppx] = "https://data.mcappx.com/v1/bedrock.json",
        [ListSourceEnum.Github] =
            "https://raw.githubusercontent.com/Open-MBC/-MineCraft-Bedrock-Download-SU/refs/heads/main/bedrock.json"
    };
}