using System;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Interface.ModLoader;

public interface IModsLoader
{
    public string LoaderName { get; }
    public string LoaderDescription { get; }
    public bool CanRemove { get; }
    public bool IsAllowDisabling { get; }
    public string? IconUri { get; }
    public string ModsFolder { get; }
    public VersionConfig GameInstance { get; set; }
    public void InitLoader(VersionConfig instance);
    public void PreLaunch();
    public Task<bool> ApplicableInstance();
    public string GetInstalledVersion();
    public bool IsInstalled();
    public void Install();
    public void Remove();
    public void ViewInfo();
    public Action? OnUpdate { get; set; }
    public IModsManager ModsManager { get; set; }
    public bool GetIsEnabled();
    public void SetIsEnabled(bool isEnabled);
}