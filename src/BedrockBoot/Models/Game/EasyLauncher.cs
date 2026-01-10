using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Models.Game;

public class EasyLauncher
{
    public VersionConfig VersionInfo { get; private set; }
    public Action? OnMigration { get; set; }
    public Action<Process>? Launched { get; set; }
    public Action? LaunchCompleted { get; set; }
    public Action<string, double>? UpdateProgress { get; set; } // 新增：更新进度回调
    public Action<string>? UpdateProgressText { get; set; } // 新增：更新进度文本回调
    public Action<bool>? SetProgressIndeterminate { get; set; } // 新增：设置进度条是否为不确定模式
    public Process MinecraftProcess { get; private set; }
    private ModsCore _core;

    public EasyLauncher(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig;
    }

    public async Task Launch()
    {
        if (GlobalModel.BedrockCore == null)
        {
            GlobalModel.BedrockCore = new BedrockCore()
            {
                Options = new CoreOptions()
                {
                    IsAutoCompleteVC = true,
                    IsAutoOpenDevelopment = true,
                    IsCheckMD5 = true
                }
            };
            await GlobalModel.BedrockCore.InitAsync();
        }
        _core = new ModsCore(VersionInfo);

        var args = "";

        if (VersionInfo.Config.IsEditModel) args += "minecraft://creator/?Editor=true ";
        args += VersionInfo.Config.OtherCommand;

        _core.PreLoad(); // 启动 PreLoad

        try
        {
            var iso = new IsolationCore(VersionInfo);
            iso.Init();
        }
        catch (Exception ex)
        {
            // 迁移失败，触发迁移回调
            OnMigration?.Invoke();
            return;
        }

        try
        {
            MinecraftProcess = await GlobalModel.BedrockCore.LaunchGameAsync(new LaunchOptions()
            {
                GameFolder = VersionInfo.VersionPath,
                GameType = VersionInfo.Info.VersionType,
                MinecraftBuildType = VersionInfo.Info.BuildType,
                RegisterProgress = new Progress<DeploymentProgress>((progress) =>
                {
                    Console.WriteLine($"registerProcess_percent: {progress.percentage} - {progress.state}");
                    
                    // 使用回调更新进度，而不是直接操作 UI
                    UpdateProgress?.Invoke($"步骤：{progress.state}", progress.percentage);
                }),
                Progress = new Progress<LaunchState>((state) => 
                { 
                    Console.WriteLine(state);
                    UpdateProgressText?.Invoke($"状态：{state}");
                }),
                LaunchArgs = string.IsNullOrEmpty(args) ? null : args
            });

            if (MinecraftProcess != null && !MinecraftProcess.HasExited)
            {
                Console.WriteLine($"检测到游戏启动成功 PID：{MinecraftProcess.Id}");
                
                // 触发游戏启动回调
                Launched?.Invoke(MinecraftProcess);
                
                // 更新进度文本
                UpdateProgressText?.Invoke("步骤：已启动，请等待游戏窗口显示");
                SetProgressIndeterminate?.Invoke(true);

                if (VersionInfo.Config.IsModes)
                {
                    _core.LoadAll(MinecraftProcess.Id);
                }

                // 正确注册退出事件
                MinecraftProcess.EnableRaisingEvents = true;
                MinecraftProcess.Exited += OnProcessExited;

                // 等待进程退出
                await WaitForProcessExitAsync(MinecraftProcess);
            }
            else
            {
                // 进程启动失败或立即退出
                LaunchCompleted?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动游戏时发生错误: {ex}");
            LaunchCompleted?.Invoke();
        }
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        Console.WriteLine("游戏进程已退出");
        LaunchCompleted?.Invoke();
    }

    private async Task WaitForProcessExitAsync(Process process)
    {
        try
        {
            await Task.Run(() => process.WaitForExit());
            Console.WriteLine("游戏进程已退出（异步等待）");
            LaunchCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"等待进程退出时发生错误: {ex.Message}");
            LaunchCompleted?.Invoke();
        }
    }
}