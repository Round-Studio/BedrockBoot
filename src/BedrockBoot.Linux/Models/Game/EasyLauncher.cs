using System.Diagnostics;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Helper;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Core.Models.Pack.Game.Mods;
using BedrockBoot.Models.Account.Xbox;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.GDK;
using BedrockBoot.Models.Pack.Wine;
using BedrockBoot.Proton;
using BedrockLauncher.Core;
using Round.SDK.Entity;
using Round.SDK.Plugin.BedrockBoot.Register;

namespace BedrockBoot.Models.Game;

public class EasyLauncher
{
    private ModsCore _core;
    private readonly Stopwatch _gameplayStopwatch = new();
    private DateTime _gameStartTime;
    private readonly string _playerDataFilePath;
    private readonly ProtonInfo? _linuxLaunchInfo;

    public EasyLauncher(VersionConfig versionConfig)
    {
        VersionInfo = versionConfig ?? throw new ArgumentNullException(nameof(versionConfig));
        _playerDataFilePath = Path.Combine(versionConfig.VersionPath, "playerdata.json");
        
        _linuxLaunchInfo = new ProtonInfo
        {
            PrefixPath = PathsList.PreFixPath,
            ProtonPath = ProtonCore.Config.Data.SelectProtonPath
        };

        // 隔离配置处理
        var confPath = Path.Combine(VersionInfo.VersionPath, "config", "BedrockBoot2", "config.json");
        var conf = new ConfigEntity<VersionConfig>(confPath);
        conf.Data.Config.IsVersionIsolated = false;
        conf.Save();
    }

    #region Properties & Events

    public static bool IsUseNeoLaunch { get; set; } = false;
    public static int LaunchingCount { get; private set; } = 0;
    public static Action? LaunchedBehavior { get; set; }

    public VersionConfig VersionInfo { get; }
    public Process? MinecraftProcess { get; private set; }

    // 启动生命周期回调
    public Action? NoRunTool { get; set; }
    public Action<Process>? Launched { get; set; }
    public Action? LaunchCompleted { get; set; }
    public Action<string, double>? UpdateProgress { get; set; }
    public Action<string>? UpdateProgressText { get; set; }
    public Action<bool>? SetProgressIndeterminate { get; set; }

    #endregion

    #region Public Methods

    public async Task Launch()
    {
        // 校验 Linux Proton 环境
        if (string.IsNullOrEmpty(_linuxLaunchInfo?.ProtonPath) || ProtonCore.GetInstalledVersions()?.Count <= 0)
        {
            NoRunTool?.Invoke();
            LaunchCompleted?.Invoke();
            return;
        }

        LaunchingCount++;
        bool launchingCountDecremented = false;

        try
        {
            // NeoLaunch 专属前置准备
            if (IsUseNeoLaunch)
            {
                PrepareNeoLaunchEnvironment();
            }

            // 初始化 BedrockCore 全局实例
            CoreGlobal.BedrockCore ??= new BedrockCore();

            // 更新策略配置
            VersionInfo.Config.FolderPolicyStr = IsolationPolicyHelper.ParsePolicyConfig(VersionInfo.Config.IsolationFolderPolicy);
            GameInfoHelper.SaveVersionConfig(VersionInfo);
            Console.WriteLine(@"已同步策略状态");

            _core = new ModsCore(VersionInfo);
            _core.PreLoad();

            // 检查并安装 GameInput 依赖
            EnsureGameInputInstalled();

            // 触发 SDK 注册的 LaunchingEvent 回调
            TriggerPluginLaunchingEvents();

            // 执行用户自定义启动前 Hook 命令
            if (!await TryExecutePreLaunchCommandAsync())
            {
                LaunchCompleted?.Invoke();
                return;
            }

            // 启动游戏主进程
            try
            {
                _gameplayStopwatch.Reset();
                _gameStartTime = DateTime.Now;

                var gameExecutablePath = Path.Combine(VersionInfo.VersionPath, VersionInfo.BodyFile);
                MinecraftProcess = LaunchWithProton(gameExecutablePath, allowWrapper: true);

                if (MinecraftProcess != null)
                {
                    Console.WriteLine($@"检测到游戏启动成功 PID：{MinecraftProcess.Id}");

                    LaunchingCount--;
                    launchingCountDecremented = true;

                    if (LaunchingCount == 0)
                        LaunchedBehavior?.Invoke();

                    _gameplayStopwatch.Start();
                    Console.WriteLine($@"游戏计时开始：{_gameStartTime:yyyy-MM-dd HH:mm:ss}");

                    Launched?.Invoke(MinecraftProcess);
                    UpdateProgressText?.Invoke("步骤：已启动，请等待游戏窗口显示");
                    SetProgressIndeterminate?.Invoke(true);

                    // 鼠标锁定处理
                    if (BedrockBoot.Core.Global.GlobalModel.Config.Data.IsMouseLock)
                    {
                        var mouse = new ProcessMouseLocker(MinecraftProcess.Id);
                        mouse.Start();
                    }

                    // Mod 载入
                    if (VersionInfo.Config.IsModes)
                    {
                        _core.LoadAll(MinecraftProcess.Id);
                    }

                    MinecraftProcess.EnableRaisingEvents = true;

                    // 异步监听进程退出
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
                StopAndResetTimer();
                LaunchCompleted?.Invoke();
            }
        }
        finally
        {
            if (!launchingCountDecremented)
            {
                LaunchingCount--;
            }
        }
    }

    #endregion

    #region Proton / Wine Launch Logic

    private Process? LaunchWithProton(string filePath, bool allowWrapper = false)
    {
        if (_linuxLaunchInfo == null) return null;

        return IsUseNeoLaunch 
            ? LaunchWithNeoProton(filePath) 
            : LaunchWithStandardProton(filePath, allowWrapper);
    }

    private Process? LaunchWithStandardProton(string filePath, bool allowWrapper)
    {
        if (!Directory.Exists(_linuxLaunchInfo!.PrefixPath))
            Directory.CreateDirectory(_linuxLaunchInfo.PrefixPath);

        string dosDevicesPath = Path.Combine(_linuxLaunchInfo.PrefixPath, "pfx", "dosdevices");
        if (!Directory.Exists(dosDevicesPath))
        {
            Directory.CreateDirectory(dosDevicesPath);
            Console.WriteLine($"已创建 dosdevices 目录: {dosDevicesPath}");
        }

        string protonScript = Path.Combine(_linuxLaunchInfo.ProtonPath, "proton");

        var startInfo = new ProcessStartInfo
        {
            FileName = protonScript,
            Arguments = $"run \"{filePath}\"",
            WorkingDirectory = _linuxLaunchInfo.ProtonPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // 注入 Proton 环境变量
        startInfo.EnvironmentVariables["STEAM_COMPAT_DATA_PATH"] = _linuxLaunchInfo.PrefixPath;
        startInfo.EnvironmentVariables["STEAM_COMPAT_CLIENT_INSTALL_PATH"] =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam");

        string libPath = $"{Path.Combine(_linuxLaunchInfo.ProtonPath, "files/lib64")}:{Path.Combine(_linuxLaunchInfo.ProtonPath, "files/lib")}";
        string currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
        
        startInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = $"{libPath}:{currentLdPath}";
        startInfo.EnvironmentVariables["WINEDLLOVERRIDES"] = "dxgi,d3d11,d3d10core,d3d9=b";

        // 应用用户包装器命令（如 gamemoderun）
        if (allowWrapper)
        {
            var launchCommandConfig = BedrockBoot.Core.Global.GlobalModel.Config.Data.LaunchCommandConfig;
            if (launchCommandConfig.IsEnable && !string.IsNullOrWhiteSpace(launchCommandConfig.WrapperCommand))
            {
                LaunchCommandHelper.TryApplyWrapper(startInfo, launchCommandConfig.WrapperCommand, VersionInfo);
            }
        }

        try
        {
            var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: Launching Game failed: {ex}");
            return null;
        }
    }

    private Process? LaunchWithNeoProton(string filePath)
    {
        var umu = Path.Combine(PathsList.NeoProtonPath, "umu", "umu-run");
        if (!File.Exists(umu))
        {
            Console.WriteLine($"umu-run not found at {umu}, printing launch command instead");
            return null;
        }

        var steamCompat = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam");
        Directory.CreateDirectory(steamCompat);

        var prioPath = Path.Combine(PathsList.NeoProtonPath, "etc", "gnutls-no-tls13.cfg");
        string dllOverrides = "dxgi,d3d11,d3d10core,d3d9,advapi32=b";

        var env = new Dictionary<string, string>
        {
            ["PROTONPATH"] = ProtonNeoCore.ProtonRootPath,
            ["PROTON_VERB"] = "run",
            ["WINEPREFIX"] = PathsList.PreFixPath,
            ["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = steamCompat,
            ["UMU_FOLDERS_PATH"] = PathsList.NeoProtonPath,
            ["UMU_RUNTIME_UPDATE"] = "0",
            ["GAMEID"] = "umu-default",
            ["WINEGDK_PREAUTH_DEVICE"] = "Z:" + PathsList.DeviceJsonPath.Replace("/", "\\"),
            ["GNUTLS_SYSTEM_PRIORITY_FILE"] = prioPath,
            ["WINEDEBUG"] = "-all",
            ["WINEDLLOVERRIDES"] = dllOverrides
        };

        Console.WriteLine("Launching Minecraft...");
        var psi = new ProcessStartInfo(umu, filePath)
        {
            UseShellExecute = false
        };

        foreach (var (k, v) in env)
        {
            psi.EnvironmentVariables[k] = v;
        }

        var proc = Process.Start(psi);
        if (proc == null)
        {
            Console.WriteLine("Failed to start process");
            return null;
        }

        Console.WriteLine("Minecraft is running");
        return proc;
    }

    #endregion

    #region Helper & Auxiliary Methods

    private void PrepareNeoLaunchEnvironment()
    {
        UpdateProgressText?.Invoke("正在登录账户");
        var xbl = new XblAuth();
        if (!xbl.RunPreauth(CoreInit.AccessToken))
            Console.WriteLine("Xbox Live pre-auth failed");

        UpdateProgressText?.Invoke("正在准备 Proton 环境");

        try
        {
            if (!WinePrefix.Boot())
                Console.WriteLine("Could not initialise Wine prefix");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WinePrefix.Boot failed: {ex.Message}");
        }

        try { WinePrefix.ApplyWinegdkPrereqs(); }
        catch (Exception ex) { Console.WriteLine($"ApplyWinegdkPrereqs failed: {ex.Message}"); }

        InstallCryptbase();

        try { WinePrefix.SetRefreshToken(CoreInit.RefreshToken); }
        catch (Exception ex) { Console.WriteLine($"SetRefreshToken failed: {ex.Message}"); }

        ProtonPatcher.Patch(ProtonNeoCore.ProtonRootPath);
        GameInputInstaller.Install(PathsList.PreFixPath, VersionInfo.VersionPath);

        GdkFixups.PatchLhcXcurlGate(VersionInfo.VersionPath);
        GdkFixups.BumpStackReserve(Path.Combine(VersionInfo.VersionPath, VersionInfo.BodyFile));

        var prioDir = Path.Combine(PathsList.NeoProtonPath, "etc");
        Directory.CreateDirectory(prioDir);
        var prioPath = Path.Combine(prioDir, "gnutls-no-tls13.cfg");
        File.WriteAllText(prioPath, "[priorities]\nSYSTEM = NORMAL:-VERS-TLS1.3:%COMPAT\n");
    }

    private void EnsureGameInputInstalled()
    {
        var protonConfig = ProtonCore.GetVersionConfig(_linuxLaunchInfo!.ProtonPath);
        
        bool isInstalled = VersionInfo.VersionStatus.GameInputInstalled 
            && Path.Exists(_linuxLaunchInfo.PrefixPath) 
            && protonConfig.IsGameInputInstalled;

        if (isInstalled) return;

        Console.WriteLine("正在运行 GameInput 安装...");
        var msiPath = Path.Combine(VersionInfo.VersionPath, "Installers", "GameInputRedist.msi");

        if (!IsUseNeoLaunch)
        {
            LaunchWithProton(msiPath)?.WaitForExit();
        }
        else
        {
            InstallGameInputWithWine(PathsList.PreFixPath, msiPath);
        }
        Console.WriteLine("GameInput 安装完毕");

        VersionInfo.VersionStatus.GameInputInstalled = true;
        protonConfig.IsGameInputInstalled = true;
        
        GameInfoHelper.SaveVersionConfig(VersionInfo);
        ProtonCore.SaveVersionConfig(protonConfig);
    }

    private void InstallGameInputWithWine(string prefix, string msiPath)
    {
        if (!File.Exists(msiPath)) return;

        var psi = new ProcessStartInfo
        {
            FileName = "wine",
            Arguments = $"msiexec /i \"{msiPath}\" /quiet /norestart",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.EnvironmentVariables["WINEPREFIX"] = prefix;
        psi.EnvironmentVariables["WINEDEBUG"] = "-all";
        psi.EnvironmentVariables["WINEDLLOVERRIDES"] = "advapi32=n,b";

        try
        {
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(300000); // 5分钟超时限制
                Console.WriteLine($"GameInput installation completed with exit code: {proc.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GameInput 安装时出错: {ex.Message}");
        }
    }

    private static void InstallCryptbase()
    {
        try
        {
            var builtin = Path.Combine(ProtonNeoCore.ProtonRootPath, "files", "lib", "wine", "x86_64-windows", "cryptbase.dll");
            var dst = Path.Combine(PathsList.PreFixPath, "drive_c", "windows", "system32", "cryptbase.dll");

            if (File.Exists(builtin) && !File.Exists(dst))
            {
                var directoryName = Path.GetDirectoryName(dst);
                if (directoryName != null) Directory.CreateDirectory(directoryName);
                
                File.Copy(builtin, dst);
                Console.WriteLine("cryptbase installed in prefix");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"安装 cryptbase.dll 失败: {ex.Message}");
        }
    }

    private async Task<bool> TryExecutePreLaunchCommandAsync()
    {
        var launchCommandConfig = BedrockBoot.Core.Global.GlobalModel.Config.Data.LaunchCommandConfig;
        if (!launchCommandConfig.IsEnable || string.IsNullOrWhiteSpace(launchCommandConfig.PreLaunchCommand))
            return true;

        UpdateProgressText?.Invoke("状态：正在执行启动前命令");

        var exitCode = await LaunchCommandHelper.RunHookAsync(
            launchCommandConfig.PreLaunchCommand,
            VersionInfo,
            launchCommandConfig.IsWaitForPreLaunch,
            launchCommandConfig.PreLaunchTimeout,
            "启动前命令");

        if (launchCommandConfig.IsAbortOnPreLaunchFailure && exitCode is not null and not 0)
        {
            Console.WriteLine($@"启动前命令返回非零退出码 {exitCode}，已中止启动");
            return false;
        }

        return true;
    }

    private void TriggerPluginLaunchingEvents()
    {
        foreach (var action in RegisterService.API.LaunchingEvent)
        {
            new Thread(() =>
            {
                try
                {
                    if (VersionInfo.VersionPath != null) 
                        action.Invoke(VersionInfo.VersionPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"执行启动前方法失败：{ex}");
                }
            }).Start();
        }
    }

    private async Task WaitForProcessExitAsync(Process process)
    {
        try
        {
            // 在 .NET 6+ 使用 WaitForExitAsync，性能优于 Task.Run(() => process.WaitForExit())
            await process.WaitForExitAsync();

            if (_gameplayStopwatch.IsRunning)
            {
                _gameplayStopwatch.Stop();
                TimeSpan playTime = _gameplayStopwatch.Elapsed;

                UpdatePlayerPlayTime(playTime);
                SessionStoreHelper.AddSession(VersionInfo.VersionPath, _gameStartTime, (long)playTime.TotalSeconds);
            }

            Console.WriteLine(@"游戏进程已退出（异步等待）");
            await RunPostExitCommandAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"等待进程退出时发生错误: {ex.Message}");
            StopAndResetTimer();
        }
        finally
        {
            LaunchCompleted?.Invoke();
        }
    }

    private async Task RunPostExitCommandAsync()
    {
        var config = BedrockBoot.Core.Global.GlobalModel.Config.Data.LaunchCommandConfig;
        if (!config.IsEnable || string.IsNullOrWhiteSpace(config.PostExitCommand)) return;

        await LaunchCommandHelper.RunHookAsync(
            config.PostExitCommand,
            VersionInfo,
            waitForExit: true,
            0,
            "启动后命令");
    }

    private void UpdatePlayerPlayTime(TimeSpan playTime)
    {
        var playerData = VersionInfo.PlayerData;
        playerData.TotalPlayTime += (long)playTime.TotalSeconds;
        playerData.LastPlayTime = DateTime.Now;
        playerData.FirstPlayTime ??= _gameStartTime;

        playerData.TotalSessions++;
        VersionInfo.PlayerData = playerData;
        GameInfoHelper.SaveVersionConfig(VersionInfo);
    }

    private void StopAndResetTimer()
    {
        if (_gameplayStopwatch.IsRunning)
            _gameplayStopwatch.Stop();
    }

    #endregion
}