using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Interface;

namespace BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;

public class PreLoaderNet : IModsLoader
{
    private VersionConfig _instance;
    public string LoaderName { get; } = "原版加载器";
    public string LoaderDescription { get; } = "BedrockBoot 原版加载器 (PreLoad.NET)";
    public bool CanRemove { get; } = false;
    public string? IconUri { get; } = "avares://BedrockBoot/Assets/Icon/BedrockBoot.Icon.256x.png";

    public void InitLoader(VersionConfig instance)
    {
        _instance = instance;
    }

    public void PreLaunch()
    {
    }

    public async Task<bool> ApplicableInstance() => true;

    public string GetInstalledVersion()
    {
        try
        {
            File.WriteAllBytes(Path.Combine(_instance.VersionPath!, "PreLoad.NET.dll"),
                Dependence.Dependence.GetResource("BedrockBoot.Dependence.Dependence.PreLoad.NET.dll"));

            Console.WriteLine(@"PreLoad.NET.dll 释放完毕");
        }
        catch
        {
        }

        return GetAllInstalledVersion();
    }

    public bool IsInstalled() => true;

    public void Install()
    {
        throw new Exception("您无法安装此加载器");
    }

    public void Remove()
    {
        throw new Exception("您无法卸载此加载器");
    }

    public void ViewInfo()
    {
    }

    private string GetAllInstalledVersion()
    {
        try
        {
            string dllPath = Path.Combine(_instance.VersionPath!, "PreLoad.NET.dll");

            if (!File.Exists(dllPath))
            {
                Console.WriteLine(@"DLL 文件不存在");
                return null;
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            string version = versionInfo.FileVersion ?? versionInfo.ProductVersion ?? "未知版本";

            Console.WriteLine(@$"Version: {version}");
            return version;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取版本失败: {ex.Message}");
            return null;
        }
    }
}