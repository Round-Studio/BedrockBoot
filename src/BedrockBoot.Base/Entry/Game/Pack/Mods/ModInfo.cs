using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Avalonia.Platform;

namespace BedrockBoot.Base.Entry.Game.Pack.Mods;

public class ModInfo
{
    /// <summary>
    ///     File 为模组文件
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; }

    /// <summary>
    ///     InjectDelay 为注入延迟，单位 ms
    /// </summary>
    [JsonPropertyName("injectDelay")]
    public int InjectDelay { get; set; } = 0;

    /// <summary>
    ///     IsPreLoad 为是否启用预加载
    /// </summary>
    [JsonPropertyName("isPreLoad")]
    public bool IsPreLoad { get; set; } = false;

    public void Inject(int processId)
    {
        if (IsPreLoad)
            throw new Exception("This mod is PreLoad mod.");
        
        BedrockBoot.Inject.Native.Init(Dependence.Dependence.GetResource("BedrockBoot.Dependence.Dependence.Inject.dll"));
        BedrockBoot.Inject.Native.LoadPlugins(processId, Path.GetFullPath(File), InjectDelay != 0,
            InjectDelay);
    }
}