using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Instance;

await CoreInit.Init();
var updater = new InstanceUpdater(GameInfoHelper.GetVersionConfig($"E:\\Bedrock\\bedrock_versions\\1.26.29", true))
{
    ChooseDownloadUrl = list => list[0].Url,
    Progress = new Progress<InstanceUpdateProgress>(progress =>
    {
        Console.WriteLine($@"[{progress.Step}] {progress.Message} {progress.Detailed} {progress.Progress}%");
    })
};
var lst = updater.GetUpdateableVersions();
lst.ForEach(x=>Console.WriteLine($@"{x.ID} - {x.Type} - {x.BuildType}"));

await updater.UpdateAsync(lst[0]);