using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Interface;

public interface IModsLoader
{
    public string LoaderName { get; }
    public string LoaderDescription { get; }
    public bool CanRemove { get; }
    public string? IconUri { get; }
    public void InitLoader(VersionConfig instance);
    public void PreLaunch();
    public Task<bool> ApplicableInstance();
    public string GetInstalledVersion();
    public bool IsInstalled();
    public void Install();
    public void Remove();
    public void ViewInfo();
}