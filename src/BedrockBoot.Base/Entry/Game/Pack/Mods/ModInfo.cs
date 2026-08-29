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

    public void Inject(int processId)
    {
        if (IsPreLoad)
            throw new Exception("This mod is PreLoad mod.");

        BedrockBoot.Inject.Native.Init(
            Dependence.Dependence.GetResource("BedrockBoot.Dependence.Dependence.Inject.dll"));
        BedrockBoot.Inject.Native.LoadPlugins(processId, Path.GetFullPath(File), InjectDelay != 0,
            InjectDelay);
    }
}