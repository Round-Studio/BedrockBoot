using System.Collections.Generic;
using System.Text.Json.Serialization;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Language;

namespace BedrockBoot.Base.Entry
{
    public class ConfigEntry
    {
        [JsonPropertyName("configVersion")] public Version? ConfigVersion { get; set; } = null;
        [JsonPropertyName("windowInfo")] public WindowInfo WindowInfo { get; set; } = new();
        [JsonPropertyName("gameFolders")] public List<GameFolderInfo> GameFolders { get; set; } = new();
        [JsonPropertyName("gameFolderSelIndex")] public int GameFolderSelIndex { get; set; } = -1;
        [JsonPropertyName("downloadChunkCount")] public int DownloadChunkCount { get; set; } = 4;
        [JsonPropertyName("versionSourceIndex")] public int VersionSourceIndex { get; set; } = 0;
        [JsonPropertyName("curseForgeSourceIndex")] public int CurseForgeSourceIndex { get; set; } = 0;
        [JsonPropertyName("styleConfig")] public StyleConfig StyleConfig { get; set; } = new();
        [JsonPropertyName("homeConfig")] public HomeConfig HomeConfig { get; set; } = new();
        [JsonPropertyName("isAutoCheckUpdate")] public bool IsAutoCheckUpdate { get; set; } = true;
        [JsonPropertyName("isFirstRun")] public bool IsFirstRun { get; set; } = true;
        [JsonPropertyName("isAgreeTerms")] public bool IsAgreeTerms { get; set; } = false;
        [JsonPropertyName("isConsole")] public bool IsConsole { get; set; } = false;
        [JsonPropertyName("isTaskBarJumpItem")] public bool IsTaskBarJumpItem { get; set; } = true;
        [JsonPropertyName("isUseHardwareDecode")] public bool IsUseHardwareDecode { get; set; } = true;
        [JsonPropertyName("isPlayBackgroundMusic")] public bool IsPlayBackgroundMusic { get; set; } = true;
        [JsonPropertyName("isEnableFuzzySearch")] public bool IsEnableFuzzySearch { get; set; } = true;
        [JsonPropertyName("updateType")] public UpdateType UpdateType { get; set; } = UpdateType.Release;
        [JsonPropertyName("isolationModel")] public IsolationType IsolationModel { get; set; } = IsolationType.Hook;
        [JsonPropertyName("isolationPriority")] public IsolationModelEnum IsolationPriority { get; set; } = IsolationModelEnum.Plus;
        [JsonPropertyName("catalogStrategy")] public CatalogStrategyEnum CatalogStrategy { get; set; } = CatalogStrategyEnum.Independence;
        [JsonPropertyName("language")] public LanguageEnum Language { get; set; } = LanguageEnum.Chinese;
        [JsonPropertyName("gatherInfo")] public bool GatherInfo { get; set; } = true;
        [JsonPropertyName("isShowConnectPage")] public bool IsShowConnectPage { get; set; } = false;
        [JsonPropertyName("isUseBetaUI")] public bool IsUseBetaUI { get; set; } = false;
        [JsonPropertyName("isMouseLock")] public bool IsMouseLock { get; set; } = false;
        [JsonPropertyName("isMouseLockForGdk")] public bool IsMouseLockForGdk { get; set; } = false;
        [JsonPropertyName("isMouseLockReserve")] public bool IsMouseLockReserve { get; set; } = false;
        [JsonPropertyName("mouseLockWindowTrimming")] public int MouseLockWindowTrimming { get; set; } = 2;
        [JsonPropertyName("isMouseLockGetFrame")] public bool IsMouseLockGetFrame { get; set; } = true;
        [JsonPropertyName("mouseLockHotkey")] public string MouseLockHotkey { get; set; } = "Ctrl+Alt";
        [JsonPropertyName("isUseSystemWindow")] public bool IsUseSystemWindow { get; set; } = false;
        [JsonPropertyName("launchBehavior")] public LaunchBehaviorEnum LaunchBehavior { get; set; } = LaunchBehaviorEnum.Normal;
        [JsonPropertyName("mediaVolume")] public double MediaVolume { get; set; } = 20;
        [JsonPropertyName("pubOptionsConfig")] public PublicOptionsConfig? PublicOptionsConfig { get; set; } = null;
        [JsonPropertyName("launchCommandConfig")] public LaunchCommandConfig LaunchCommandConfig { get; set; } = new();
    }

    /// <summary>
    /// 自定义启动命令配置（启动前命令、启动后命令、运行包装器）
    /// </summary>
    public class LaunchCommandConfig
    {
        /// <summary>总开关，关闭时所有自定义命令均不生效</summary>
        [JsonPropertyName("isEnable")] public bool IsEnable { get; set; } = false;

        /// <summary>启动前执行的命令，在游戏进程启动之前运行</summary>
        [JsonPropertyName("preLaunchCommand")] public string PreLaunchCommand { get; set; } = string.Empty;

        /// <summary>启动后执行的命令，在游戏进程退出之后运行</summary>
        [JsonPropertyName("postExitCommand")] public string PostExitCommand { get; set; } = string.Empty;

        /// <summary>
        /// 运行包装器，使用 %command% 占位符代表原始的游戏启动命令。
        /// 例如：gamemoderun %command%
        /// 仅 Linux 平台生效。
        /// </summary>
        [JsonPropertyName("wrapperCommand")] public string WrapperCommand { get; set; } = string.Empty;

        /// <summary>是否等待启动前命令执行结束后再启动游戏</summary>
        [JsonPropertyName("isWaitForPreLaunch")] public bool IsWaitForPreLaunch { get; set; } = true;

        /// <summary>启动前命令等待超时时间（秒），0 表示无限等待</summary>
        [JsonPropertyName("preLaunchTimeout")] public int PreLaunchTimeout { get; set; } = 30;

        /// <summary>启动前命令执行失败（非零退出码）时是否中止启动</summary>
        [JsonPropertyName("isAbortOnPreLaunchFailure")] public bool IsAbortOnPreLaunchFailure { get; set; } = false;
    }

    public class PublicOptionsConfig
    {
        [JsonPropertyName("pubOptionsInstance")] public string? PubOptionsInstancePath { get; set; } = string.Empty;
        [JsonPropertyName("pubUser")] public string? PubUser { get; set; } = string.Empty;
    }

    public class StyleConfig
    {
        [JsonPropertyName("isUseThemePack")] public bool IsUseThemePack { get; set; } = false;
        [JsonPropertyName("selectThemePackHash")] public string SelectThemePackHash { get; set; } = string.Empty;
        [JsonPropertyName("lightThemeType")] public ThemeModelEnum LightThemeType { get; set; } = ThemeModelEnum.Dark;
        [JsonPropertyName("backgroundMusic")] public string BackgroundMusic { get; set; } = string.Empty;
        [JsonPropertyName("backgroundImage")] public string BackgroundImage { get; set; } = string.Empty;
        [JsonPropertyName("backgroundImageOpacity")] public int BackgroundImageOpacity { get; set; } = 100;
        [JsonPropertyName("backgroundImageBlur")] public int BackgroundImageBlur { get; set; } = 1;
        [JsonPropertyName("background3d")] public bool Background3D { get; set; } = false;
        [JsonPropertyName("liveOpacity")] public int LiveOpacity { get; set; } = 40;
        [JsonPropertyName("liveBlur")] public bool LiveBlur { get; set; } = false;
        [JsonPropertyName("styleType")] public StyleType StyleType { get; set; } = StyleType.Voronoi;
        [JsonPropertyName("accentColorIndex")] public int AccentColorIndex { get; set; } = 36;
        [JsonPropertyName("mediaSource")] public MediaSourceEnum MediaSource { get; set; } = MediaSourceEnum.PriorityThemePack;
        [JsonPropertyName("mainFont")] public string MainFont { get; set; } = "DINPro";
        [JsonPropertyName("fallbackFont")] public string FallbackFont { get; set; } = "Noto Sans SC";
    }

    public class HomeConfig
    {
        [JsonPropertyName("homeType")] public HomeType HomeType { get; set; } = HomeType.None;
        [JsonPropertyName("homeXmlFiles")] public List<string> HomeXmlFiles { get; set; } = new();
        [JsonPropertyName("homeXmlSelIndex")] public int HomeXmlSelIndex { get; set; } = -1;
    }
}