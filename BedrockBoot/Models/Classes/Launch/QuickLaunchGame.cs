using BedrockBoot.Models.Classes.Helper;
using BedrockBoot.Native;
using BedrockBoot.Tools;
using BedrockBoot.Versions;
using BedrockLauncher.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Management.Deployment;

namespace BedrockBoot.Models.Classes.Launch
{
    public class QuickLaunchGame
    {
        public static void LaunchGame(NowVersions versionInfo)
        {
            MouseHelper.StopMouseLock();

            MouseHelper.AddTargetWindow(versionInfo.VersionName);

            if (global_cfg.cfg.JsonCfg.MouseLock)
            {
                MouseHelper.StartMouseLock();
            }

            Task.Run((() =>
            {
                if (!Directory.Exists(Path.Combine(versionInfo.Version_Path, "mods")))
                {
                    Directory.CreateDirectory(Path.Combine(versionInfo.Version_Path, "mods"));
                }

                var packageManager = new PackageManager();
                var findPackages = packageManager.FindPackages();
                bool hasPackage = false;
                foreach (var package in findPackages)
                {
                    if (package.InstalledPath == versionInfo.Version_Path)
                    {
                        hasPackage = true;
                    }
                }

                if (hasPackage == true)
                {
                    globalTools.ShowInfo("正在启动中 " + versionInfo.DisPlayName);
                    global_cfg.core.LaunchGame(versionInfo.Type switch
                    {
                        "Release" => VersionType.Release,
                        "Preview" => VersionType.Preview,
                        "Beta" => VersionType.Beta
                    });
                    StartInjectThread(versionInfo.Version_Path);
                    return;
                }
                var installCallback = new InstallCallback()
                {
                    registerProcess_percent = ((s, u) =>
                    {
                        Debug.WriteLine(u);
                    }),
                    result_callback = ((status, exception) =>
                    {
                        if (exception != null)
                        {
                            Debug.WriteLine(exception);
                            throw exception;
                        }
                    })
                };
                globalTools.ShowInfo("正在注册版本中请耐心等待" + versionInfo.DisPlayName);
                global_cfg.core.RemoveGame(versionInfo.Type switch
                {
                    "Release" => VersionType.Release,
                    "Preview" => VersionType.Preview,
                    "Beta" => VersionType.Beta
                });
                var changeVersion = global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);
                Debug.WriteLine(changeVersion);
                globalTools.ShowInfo("启动中 " + versionInfo.DisPlayName);
                global_cfg.core.LaunchGame(versionInfo.Type switch
                {
                    "Release" => VersionType.Release,
                    "Preview" => VersionType.Preview,
                    "Beta" => VersionType.Beta
                });
                StartInjectThread(versionInfo.Version_Path);
            }));
        }

        public static void StartInjectThread(string path)
        {
            string delay_mods_dir = Path.Combine(path, "d_mods");
            var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
            foreach (var dllFileInfo in dllFileInfos)
            {
                var thread = new Thread(() =>
                {
                    WindowsApi.Inject("Minecraft.Windows.exe", dllFileInfo.FullPath, true, global_cfg.cfg.JsonCfg.DelayTimes);
                    globalTools.ShowInfo($"注入 {dllFileInfo.FileName}");
                });
                thread.Start();
            }
        }
    }
}
