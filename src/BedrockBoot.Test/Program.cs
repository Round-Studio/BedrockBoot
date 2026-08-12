using BedrockBoot.Base.Enum.Game;
using BedrockBoot.Downloader.Game;

var version = VersionHelper.GetVersionBuildInfoList(GameInstallType.Modern);

version.ForEach(v =>
{
    var name = string.IsNullOrEmpty(v.Id) ? "滚木" : v.Id;
    Console.WriteLine($"{name} {v.Version} {v.GameBuildType}");
});