using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Threading;
using Windows.Management.Deployment;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.Isolation;
using BedrockBoot.Models.Pack.Game.Mods;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Models.Game;

public class EasyLauncher
{
    private ModsCore _core;
    private IsolationCore IsolationCore;
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
        if (GlobalModel.BedrockCore == null)
        {
            GlobalModel.BedrockCore = new BedrockCore
            {
                Options = new CoreOptions
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

        RegisterService.API.LaunchingEvent.ForEach(action =>
            new Thread(() => action.Invoke(VersionInfo.VersionPath)).Start());

        try
        {
            // 重置计时器
            _gameplayStopwatch.Reset();
            _gameStartTime = DateTime.Now;
            
            MinecraftProcess = await GlobalModel.BedrockCore.LaunchGameAsync(new LaunchOptions
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
                
                if (GlobalModel.Config.Data.IsMouseLock)
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
}