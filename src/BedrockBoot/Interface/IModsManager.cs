using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Interface;

public interface IModsManager
{
    public void Init(VersionConfig instance);
    public Action? OnRefresh { get; set; }
    public List<ModItemInfo> GetAllMods();
    public Task AddMod();
    public void Remove(ModItemInfo info);
}

public class ModItemInfo
{
    public string? ModName { get; set; }
    public string? ModDescription { get; set; }
    public string? ModPath { get; set; }
    public string? Version { get; set; }
    public Type? ModLoaderType { get; set; }
    public ModType ModInjectType { get; set; }
    public int InjectDelay { get; set; } = 5000;
}