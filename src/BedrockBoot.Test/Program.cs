using BedrockBoot.Models.Helper.Uwp;

var lst = UwpDependencyChecker.GetMissingDependencies();
foreach (var item in lst)
{
    Console.WriteLine(item.Item1);
    var result = await UwpFileUrl.GetUwpPackageDownloadUrl(item.Item1, item.Item2);
    Console.WriteLine(result);
}