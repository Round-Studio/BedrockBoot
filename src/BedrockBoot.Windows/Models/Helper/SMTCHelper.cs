using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsMediaController;
using Windows.Media.Control;

namespace BedrockBoot.Models.Helper;

public static class SMTCHelper
{
    private static MediaManager? _mediaManager;
    private static bool _isStarted;

    /// <summary>
    /// MediaManager 是否已启动
    /// </summary>
    public static bool IsStarted => _isStarted;

    /// <summary>
    /// 初始化并启动 MediaManager（建议在程序启动时调用一次）
    /// </summary>
    public static void Initialize()
    {
        _mediaManager = new MediaManager();
        _mediaManager.Start();
        _isStarted = true;
    }

    /// <summary>
    /// 停止并释放 MediaManager
    /// </summary>
    public static void Shutdown()
    {
        if (_mediaManager != null)
        {
            _mediaManager.Dispose();
            _mediaManager = null;
        }
        _isStarted = false;
    }

    /// <summary>
    /// 获取当前焦点会话
    /// </summary>
    public static MediaManager.MediaSession? GetCurrentSession()
    {
        if (!_isStarted || _mediaManager == null)
            return null;

        return _mediaManager.GetFocusedSession();
    }

    /// <summary>
    /// 获取当前媒体属性（标题、歌手、专辑等）
    /// </summary>
    public static async Task<(string? Title, string? Artist, string? Album)?> GetMediaPropertiesAsync()
    {
        var session = GetCurrentSession();

        if (session == null)
            return null;

        try
        {
            var props = await session.ControlSession.TryGetMediaPropertiesAsync();
            return (props.Title, props.Artist, props.AlbumTitle);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取播放状态
    /// </summary>
    public static GlobalSystemMediaTransportControlsSessionPlaybackStatus? GetPlaybackStatus()
    {
        var session = GetCurrentSession();

        if (session == null)
            return null;

        try
        {
            var playbackInfo = session.ControlSession.GetPlaybackInfo();
            return playbackInfo.PlaybackStatus;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public static bool IsPlaying()
    {
        var status = GetPlaybackStatus();
        return status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
    }

    /// <summary>
    /// 获取正在播放的源程序名称
    /// 返回示例："Spotify.exe"、"Microsoft.Edge.Stable_..." 等
    /// </summary>
    public static string? GetSourceAppName()
    {
        var session = GetCurrentSession();

        if (session?.ControlSession == null)
            return null;

        try
        {
            // 使用 Windows 原生的 SourceAppUserModelId 代替不存在的 SourceAppInfo
            var aumid = session.ControlSession.SourceAppUserModelId;
            
            if (string.IsNullOrEmpty(aumid))
                return "未知程序";

            // 如果是 Win32 程序的完整路径或带有 .exe，提取出纯文件名
            if (aumid.Contains('\\') || aumid.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return System.IO.Path.GetFileName(aumid);
            }

            return aumid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取源程序名称失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取源程序的完整信息（程序名 + 进程ID）
    /// </summary>
    public static (string? AppName, int? ProcessId) GetSourceAppDetails()
    {
        var appName = GetSourceAppName();
        return (appName, null);
    }
}