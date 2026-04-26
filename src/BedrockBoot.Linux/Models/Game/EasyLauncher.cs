using System;
using System.Diagnostics;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Proton;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using Round.SDK.Entity;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Models.Game;

public class EasyLauncher
{
    private ModsCore _core;
    private Stopwatch _gameplayStopwatch; // 计时器
    private DateTime _gameStartTime; // 游戏开始时间
    private string _playerDataFilePath; // 玩家数据文件路径
    
    private static int LaunchingCount { get; set; } = 0;

    // Linux Proton 启动所需的字段
    private readonly ProtonInfo? _linuxLaunchInfo;

    public EasyLauncher(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig;
        _gameplayStopwatch = new Stopwatch(); // 初始化计时器
        _playerDataFilePath = Path.Combine(versionConfig.VersionPath, "playerdata.json"); // 玩家数据文件路径
        _linuxLaunchInfo = new ProtonInfo()
        {
            PrefixPath = Path.Combine(PathsList.ProtonPath, "game_prefix"),
            ProtonPath = ProtonCore.Config.Data.SelectProtonPath
        };

        var conf = new ConfigEntity<VersionConfig>(Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2",
            "config.json"));
        conf.Data.Config.IsVersionIsolated = false;
        conf.Save();
    }

    public VersionConfig VersionInfo { get; }
    public Action? NoRunTool { get; set; }
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

    // Linux Proton 启动方法
    private Process? LaunchWithProton(string filePath)
    {
        if (_linuxLaunchInfo == null)
            return null;

        if (!Directory.Exists(_linuxLaunchInfo.PrefixPath)) 
            Directory.CreateDirectory(_linuxLaunchInfo.PrefixPath);

        string protonScript = Path.Combine(_linuxLaunchInfo.ProtonPath, "proton");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = protonScript,
            Arguments = $"run \"{filePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // 注入 Proton 所需的环境变量
        startInfo.EnvironmentVariables["STEAM_COMPAT_DATA_PATH"] = _linuxLaunchInfo.PrefixPath;
        startInfo.EnvironmentVariables["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam");
        
        string libPath = $"{Path.Combine(_linuxLaunchInfo.ProtonPath, "files/lib64")}:{Path.Combine(_linuxLaunchInfo.ProtonPath, "files/lib")}";
        string currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
        startInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = $"{libPath}:{currentLdPath}";
        startInfo.EnvironmentVariables["WINEDLLOVERRIDES"] = "dxgi,d3d11,d3d10core,d3d9=b";

        try
        {
            var process = new Process();
            process.StartInfo = startInfo;

            // 标准输出回调
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                }
            };

            // 错误输出回调
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                }
            };

            process.Start();
            
            // 开启异步读取流
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL:Launching Game failed: {ex}");
            return null;
        }
    }

    public async Task Launch()
    {
        if (string.IsNullOrEmpty(_linuxLaunchInfo.ProtonPath) ||
            ProtonCore.GetInstalledVersions()!.Count <= 0)
        {
            NoRunTool?.Invoke();
            LaunchCompleted?.Invoke();
            return;
        }

        LaunchingCount++;
        if (CoreGlobal.BedrockCore == null)
        {
            CoreGlobal.BedrockCore = new BedrockCore();
        }

        _core = new ModsCore(VersionInfo);

        var args = "";

        if (VersionInfo.Config.IsEditModel) args += "minecraft://creator/?Editor=true ";
        args += VersionInfo.Config.OtherCommand;

        _core.PreLoad(); // 启动 PreLoad

        RegisterService.API.LaunchingEvent.ForEach(action =>
            new Thread(() => action.Invoke(VersionInfo.VersionPath)).Start());

        if (!VersionInfo.VersionStatus.GameInputInstalled ||
            !Path.Exists(_linuxLaunchInfo?.PrefixPath))
        {
            Console.WriteLine("正在运行 GameInput 安装...");
            LaunchWithProton(Path.Combine(VersionInfo.VersionPath, "Installers", "GameInputRedist.msi"))?.WaitForExit();
            Console.WriteLine("GameInput 安装完毕");

            VersionInfo.VersionStatus.GameInputInstalled = true;
            GameInfoHelper.SaveVersionConfig(VersionInfo);
        }

        try
        {
            // 重置计时器
            _gameplayStopwatch.Reset();
            _gameStartTime = DateTime.Now;
            
            MinecraftProcess = LaunchWithProton(Path.Combine(VersionInfo.VersionPath, VersionInfo.BodyFile));

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