using System;

namespace BedrockBoot.Base.Entry;

public class OtherLauncherInfo
{
    public string Name { get; set; }
    public string IconUrl { get; set; }
    public string ConfigFile { get; set; }
    public Action<string>? OnImport { get; set; }
    public bool IsExists { get; set; } = true;
}