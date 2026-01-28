using System.Collections.Generic;

namespace BedrockBoot.Base.Entry.Info;

public class SearchResultItemInfo
{
    public string IconUri { get; set; } = string.Empty;
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Labels { get; set; } = new();
}