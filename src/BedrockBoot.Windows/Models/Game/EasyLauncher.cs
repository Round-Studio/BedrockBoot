using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Threading;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using PeNet;
using PeNet.Header.Pe;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Models.Game;

public class EasyLauncher
{
    private ModsCore _core;
    private Stopwatch _gameplayStopwatch; // 计时器
    private DateTime _gameStartTime; // 游戏开始时间
    private string _playerDataFilePath; // 玩家数据文件路径
    
    private static int LaunchingCount { get; set; } = 0;

    public EasyLauncher(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig;
        _gameplayStopwatch = new Stopwatch(); // 初始化计时器
        _playerDataFilePath = Path.Combine(versionConfig.VersionPath, "playerdata.json"); // 玩家数据文件路径
    }

    public VersionConfig VersionInfo { get; }
    public Action? OnMigration { get; set; }
    public Action<Process>? Launched { get; set; }
    public Action? LaunchCompleted { get; set; }
    public Action<string, double>? UpdateProgress { get; set; }
    public Action<string>? UpdateProgressText { get; set; }
    public Action<bool>? SetProgressIndeterminate { get; set; }
    public Process MinecraftProcess { get; private set; }
    public static Action? LaunchedBehavior { get; set; }
    private void UpdatePlayerPlayTime(TimeSpan playTime)
    {
        var playerData = VersionInfo.PlayerData;
        playerData.TotalPlayTime += (long)playTime.TotalSeconds;
        playerData.LastPlayTime = DateTime.Now;
        if (playerData.FirstPlayTime == null)
            playerData.FirstPlayTime = _gameStartTime;
        
        playerData.TotalSessions++;
        VersionInfo.PlayerData = playerData;
        GameInfoHelper.SaveVersionConfig(VersionInfo);
    }

    public async Task Launch()
    {
        LaunchingCount++;
        if (CoreGlobal.BedrockCore == null)
        {
            CoreGlobal.BedrockCore = new BedrockCore
            {
                Options = new CoreOptions
                {
                    IsAutoCompleteVC = true,
                    IsAutoOpenDevelopment = true,
                    IsCheckMD5 = true
                }
            };
            await CoreGlobal.BedrockCore.InitAsync();
        }
        
        Console.WriteLine(@"开始检测 GameService 安装状态");

        var gameServiceInstallStatue = IsGamingServicesInstalled();
        Console.WriteLine($@"GameService 安装状态：{gameServiceInstallStatue}");

        if (!gameServiceInstallStatue)
        {
            LaunchCompleted?.Invoke();
            GameServiceNotice.UnInstallGameService();
            return;
        }

        if (VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.UWP)
        {
            Console.WriteLine(@"当前实例为 UWP 构建类型，需要检测开发者模式。");
            var devMod = DeveloperModeHelper.IsDeveloperModeViaPowerShell();
            
            Console.WriteLine(@"开发者模式启用状态: " + devMod);
            if (!devMod)
            {
                LaunchCompleted?.Invoke();
                DeveloperModeHelper.ShowNotice();
                return;
            }
        }

        _core = new ModsCore(VersionInfo);

        var args = "";

        if (VersionInfo.Config.IsEditModel) args += "minecraft://creator/?Editor=true ";
        args += VersionInfo.Config.OtherCommand;

        _core.PreLoad(); // 启动 PreLoad

        RegisterService.API.LaunchingEvent.ForEach(action =>
            new Thread(() => action.Invoke(VersionInfo.VersionPath)).Start());

        if (!VersionInfo.VersionStatus.GameInputInstalled &&
            VersionInfo.Info.BuildType == MinecraftBuildTypeVersion.GDK)
        {
            Console.WriteLine(@"正在运行 GameInput 安装，请等待安装完成...");
    
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                // 使用 /qb 显示基本进度界面，/qn 是完全静默
                Arguments = $"/i \"{Path.Combine(VersionInfo.VersionPath, "Installers", "GameInputRedist.msi")}\" /qb",
                UseShellExecute = true,
                Verb = "runas"
            };
    
            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
                if (process?.ExitCode == 0)
                {
                    Console.WriteLine(@"GameInput 安装完毕");
                }
                else
                {
                    Console.WriteLine($@"GameInput 安装失败，错误码: {process?.ExitCode}");
                }
            }

            VersionInfo.VersionStatus.GameInputInstalled = true;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }

        try
        {
            // 重置计时器
            _gameplayStopwatch.Reset();
            _gameStartTime = DateTime.Now;
            
            MinecraftProcess = await CoreGlobal.BedrockCore.LaunchGameAsync(new LaunchOptions
            {
                GameFolder = VersionInfo.VersionPath,
                GameType = VersionInfo.Info.VersionType,
                MinecraftBuildType = VersionInfo.Info.BuildType,
                RegisterProgress = new Progress<DeploymentProgress>(progress =>
                {
                    Console.WriteLine($@"registerProcess_percent: {progress.percentage} - {progress.state}");

                    // 使用回调更新进度，而不是直接操作 UI
                    UpdateProgress?.Invoke($"步骤：{progress.state}", progress.percentage);
                }),
                Progress = new Progress<LaunchState>(state =>
                {
                    Console.WriteLine(state);
                    UpdateProgressText?.Invoke($"状态：{state}");
                    
                    // 当游戏启动状态变化时，更新进度文本
                    if (state == LaunchState.Launched)
                    {
                        UpdateProgressText?.Invoke("状态：游戏启动完成，开始计时");
                    }
                }),
                LaunchArgs = string.IsNullOrEmpty(args) ? null : args
            });

            if (MinecraftProcess != null)
            {
                Console.WriteLine($@"检测到游戏启动成功 PID：{MinecraftProcess.Id}");
                
                LaunchingCount--;
                if (LaunchingCount == 0) LaunchedBehavior?.Invoke();
                
                // 开始计时
                _gameplayStopwatch.Start();
                Console.WriteLine($@"游戏计时开始：{_gameStartTime:yyyy-MM-dd HH:mm:ss}");
                
                Launched?.Invoke(MinecraftProcess);
                UpdateProgressText?.Invoke("步骤：已启动，请等待游戏窗口显示");
                SetProgressIndeterminate?.Invoke(true);
                
                if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLock)
                {
                    var mouse = new ProcessMouseLocker(MinecraftProcess.Id);
                    mouse.Start();
                }

                if (VersionInfo.Config.IsModes) _core.LoadAll(MinecraftProcess.Id);

                MinecraftProcess.EnableRaisingEvents = true;

                await WaitForProcessExitAsync(MinecraftProcess);
            }
            else
            {
                LaunchCompleted?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"启动游戏时发生错误: {ex}");
            
            // 确保计时器停止
            if (_gameplayStopwatch.IsRunning)
                _gameplayStopwatch.Stop();
                
            LaunchCompleted?.Invoke();
        }
    }

    private async Task WaitForProcessExitAsync(Process process)
    {
        try
        {
            await Task.Run(() => process.WaitForExit());
            
            // 停止计时并记录数据
            if (_gameplayStopwatch.IsRunning)
            {
                _gameplayStopwatch.Stop();
                TimeSpan playTime = _gameplayStopwatch.Elapsed;
                
                // 更新玩家数据
                UpdatePlayerPlayTime(playTime);
                
                // 记录本次会话到独立文件
                SessionStoreHelper.AddSession(VersionInfo.VersionPath, _gameStartTime, (long)playTime.TotalSeconds);
            }
            
            Console.WriteLine(@"游戏进程已退出（异步等待）");
            LaunchCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"等待进程退出时发生错误: {ex.Message}");
            
            // 确保计时器停止
            if (_gameplayStopwatch.IsRunning)
                _gameplayStopwatch.Stop();
                
            LaunchCompleted?.Invoke();
        }
    }
    
    public static bool IsGamingServicesInstalled()
    {
        try
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                
                process.StartInfo.Arguments = "-NoProfile -Command \"Get-AppxPackage Microsoft.GamingServices\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
            
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("Microsoft.GamingServices");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"检测失败: {ex.StackTrace}");
            return false;
        }
    }
}