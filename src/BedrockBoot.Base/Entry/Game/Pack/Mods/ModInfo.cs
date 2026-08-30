using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Avalonia.Platform;

namespace BedrockBoot.Base.Entry.Game.Pack.Mods;

public class ModInfo
{
    [JsonPropertyName("file")] public string File { get; set; }
    [JsonPropertyName("injectDelay")] public int InjectDelay { get; set; } = 0;
    [JsonPropertyName("isPreLoad")] public bool IsPreLoad { get; set; } = false;
}