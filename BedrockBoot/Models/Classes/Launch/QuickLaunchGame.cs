using BedrockBoot.Models.Classes.Helper;
using BedrockBoot.Native;
using BedrockBoot.Tools;
using BedrockBoot.Versions;
using BedrockLauncher.Core;
using Microsoft.UI.Dispatching;
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
            MouseHelper.BORDER_MARGIN = global_cfg.cfg.JsonCfg.MouseLockCutPX;

            MouseHelper.AddTargetWindow(versionInfo.VersionName);

            if (global_cfg.cfg.JsonCfg.MouseLock)
            {
                MouseHelper.StartMouseLock();
            }

            Task.Run((() =>
            {
                try
                {
                    int count = 0;
                    if (File.Exists(Path.Combine(versionInfo.Version_Path, "CONCRT140_APP.dll")))
                    {
                        File.Delete(Path.Combine(versionInfo.Version_Path, "CONCRT140_APP.dll"));
                    }
                    bool is_register = false;
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
                        WindowsApi.LoadFix();
                        StartInject(versionInfo.Version_Path);
                        StartInject(versionInfo.Version_Path);
                        return;
                    }
                    string processName = "Minecraft.Windows"; // 注意：不需要 .exe 扩展名
                    Process[] processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        process.Kill();
                    }

                    bool Launched = false;
                    var installCallback = new InstallCallback()
                    {
                        registerProcess_percent = ((s, u) =>
                        {
                            Debug.WriteLine(u);
                            if (u >= 95)
                            {
                                count++;
                                if (count >= 2 && !Launched)
                                {
                                    global_cfg.core.LaunchGame(versionInfo.Type switch
                                    {
                                        "Release" => VersionType.Release,
                                        "Preview" => VersionType.Preview,
                                        "Beta" => VersionType.Beta
                                    });
                                    WindowsApi.LoadFix();
                                    StartInject(versionInfo.Version_Path);
                                    StartInject(versionInfo.Version_Path);
                                    Launched = true;
                                }
                            }
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

                    var _ = global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);

                }
                catch (System.Exception exception)
                {

                }


            }));
        }

        private static void StartInject(string path)
        {
            Task.Run((() =>
            {
                List<DllFileInfo> dllFileInfos = globalTools.GetDllFiles(Path.Combine(path, "d_mods"));
                Process process = null;
                while (true)
                {

                    var firstOrDefault = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
                    if (firstOrDefault == null)
                    {
                        continue;
                    }
                    else
                    {
                        process = firstOrDefault;
                        break;
                    }
                }
                foreach (var info in dllFileInfos)
                {
                    Injector.InjectProcess(process, info.FullPath);
                }
            }));

        }
    }
}
