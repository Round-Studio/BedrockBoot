using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Archive;

var info = ArchiveCheck.GetInfo(
    $"D:\\BedrockBoot\\bedrock_versions\\1.26.2\\config\\BedrockBoot2\\isolation\\Users\\2818413420751248947\\games\\com.mojang\\minecraftWorlds\\LkQVeDfcdM0=",
    "D:\\BedrockBoot\\bedrock_versions\\1.26.2");
var archiveMani = new ArchiveBackup();
/*await archiveMani.BackupAsync(info, new Progress<string>((s) =>
{
    Console.WriteLine($@"备份进度：{s}");
}));*/

Console.WriteLine(archiveMani.GetArchiveBackupsWhitUuid("c85dc64f-7939-4e39-9447-19e698b54c9d")?.Backups.Count);