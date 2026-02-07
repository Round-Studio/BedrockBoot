using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Core.Interface.Instance;

public interface IInstancePlugin
{
    public string Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public void Init(VersionConfig versionConfig);
    public bool IsInstalled();
    public Task Install();
}