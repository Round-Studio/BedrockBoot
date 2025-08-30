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
using Windows.Foundation;
using Windows.Management.Deployment;
using BedrockBoot.Controls.FlyoutContent;

namespace BedrockBoot.Models.Classes.Launch
{
    public class QuickLaunchGame
    {
        public static void LaunchGame(NowVersions versionInfo,Action<string,int> launchCallBack)
        {
            #region 启动鼠标锁

            MouseHelper.StopMouseLock();
            MouseHelper.BORDER_MARGIN = global_cfg.cfg.JsonCfg.MouseLockCutPX;

            MouseHelper.AddTargetWindow(versionInfo.VersionName);

            if (global_cfg.cfg.JsonCfg.MouseLock)
            {
                MouseHelper.StartMouseLock();
            }

            #endregion

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
                // globalTools.ShowInfo("正在启动中 " + versionInfo.DisPlayName);

                global_cfg.core.LaunchGame(versionInfo.Type switch
                {
                    "Release" => VersionType.Release,
                    "Preview" => VersionType.Preview,
                    "Beta" => VersionType.Beta
                });
                launchCallBack.Invoke("已启动",100);
                WindowsApi.LoadFix();
                StartInjectDirect(versionInfo.Version_Path);
                StartInjectThread(versionInfo.Version_Path);
                return;
            }
                    
            foreach (var process in Process.GetProcessesByName("\"Minecraft.Windows"))  // 注意：不需要 .exe 扩展名 // ↑ 谁问你了
            {
                process.Kill();
            }

            bool Launched = false;
            var installCallback = new InstallCallback()
            {
                registerProcess_percent = ((s, u) =>
                {
                    Debug.WriteLine(u);
                    launchCallBack.Invoke(s, (int)(u * 0.8));
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
                            StartInjectDirect(versionInfo.Version_Path);
                            StartInjectThread(versionInfo.Version_Path);
                            Launched = true;
                        }
                    }
                }),
                result_callback = ((status, exception) =>
                {
                    switch (status)
                    {
                        case AsyncStatus.Canceled:
                            launchCallBack.Invoke("已取消", 100);
                            break;
                        case AsyncStatus.Completed:
                            launchCallBack.Invoke("已中断", 100);
                            break;
                        case AsyncStatus.Error:
                            launchCallBack.Invoke("出现错误", 100);
                            break;
                        case AsyncStatus.Started:
                            launchCallBack.Invoke("已启动", 100);
                            break;
                    }

                    if (exception != null)
                    {
                        // Debug.WriteLine(exception);
                        launchCallBack.Invoke("出现错误", 100);
                        MessageBox.ShowAsync(exception.ToString(), "抱歉，我们出现了错误");
                    }
                })
            };
            // globalTools.ShowInfo("正在注册版本中请耐心等待" + versionInfo.DisPlayName);

            var _ = global_cfg.core.ChangeVersion(versionInfo.Version_Path, installCallback);
        }

        private static void StartInjectThread(string path)
        {
            string delay_mods_dir = Path.Combine(path, "d_mods");
            var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
            foreach (var dllFileInfo in dllFileInfos)
            {
                var thread = new Thread(() =>
                {
                    WindowsApi.Inject("Minecraft.Windows.exe", dllFileInfo.FullPath, true,
                        global_cfg.cfg.JsonCfg.DelayTimes);
                    globalTools.ShowInfo($"注入 {dllFileInfo.FileName}");
                });
                thread.Start();
            }
        }

        public static Process? WaitForMinecraftProcess(int timeoutSec = 60)
        {
            var end = DateTime.Now.AddSeconds(timeoutSec);
            while (DateTime.Now < end)
            {
                var proc = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
                if (proc != null) return proc;
                Thread.Sleep(100);
            }

            return null;
        }

        private static void StartInjectDirect(string path)
        {
            string delay_mods_dir = Path.Combine(path, "mods");
            var dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
            Process process = WaitForMinecraftProcess(60);
            foreach (var dllFileInfo in dllFileInfos)
            {
                var thread = new Thread(() =>
                {
                    Injector.InjectProcess(process, dllFileInfo.FullPath);
                    globalTools.ShowInfo($"注入 {dllFileInfo.FileName}");
                });
                thread.Start();
            }
        }
    }
}