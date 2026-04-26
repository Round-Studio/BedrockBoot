using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Proton.Enum;

ProtonCore.InitializeEnvironment();
/*((await ProtonCore.GetInstallableVersion(ProtonSource.WeatherOS))!).ToList().ForEach(re=>Console.WriteLine($"{re.Name} {re.ReleaseUrl}"));
((await ProtonCore.GetInstallableVersion(ProtonSource.LukasPAH))!).ToList().ForEach(re=>Console.WriteLine($"{re.Name} {re.ReleaseUrl}"));*/

var info = (await ProtonCore.GetInstallableVersion(ProtonSource.LukasPAH))!.ToList()[0];
await ProtonCore.InstallProton(info,new InstallInfo()
{
    InstallName = "测试 Proton1",
    IsOverWrite = true
},new Progress<DownloadProgress>(p=>Console.WriteLine($"{p.Message} {p.ProgressPercentage:F2}")));

ProtonCore.GetInstalledVersions()?.ToList().ForEach(p=>Console.WriteLine($"{p.Name} {p.Version}"));