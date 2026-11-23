/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Runtime.InteropServices.WindowsRuntime;
using BedrockBoot.Standard.Entity;
using Windows.Media.Control;
using WindowsMediaController;

namespace BedrockBoot.Windows.Models.Helper;

public static class SMTCHelper
{
    private static MediaManager? _mediaManager;
    private static bool _isStarted;

    /// <summary>
    /// MediaManager 是否已启动
    /// </summary>
    public static bool IsStarted => _isStarted;

    /// <summary>
    /// 任一会话的曲目信息发生变化（含切歌）
    /// </summary>
    public static event Action? MediaTrackChanged;

    /// <summary>
    /// 任一会话的播放状态发生变化（播放/暂停/停止）
    /// </summary>
    public static event Action? PlaybackStateChanged;

    /// <summary>
    /// 初始化并启动 MediaManager（建议在程序启动时调用一次，幂等）
    /// </summary>
    public static void Initialize()
    {
        if (_isStarted) return;

        _mediaManager = new MediaManager();
        _mediaManager.OnAnyMediaPropertyChanged += OnAnyMediaPropertyChanged;
        _mediaManager.OnAnyPlaybackStateChanged += OnAnyPlaybackStateChanged;
        _mediaManager.OnFocusedSessionChanged += OnFocusedSessionChanged;
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
            _mediaManager.OnAnyMediaPropertyChanged -= OnAnyMediaPropertyChanged;
            _mediaManager.OnAnyPlaybackStateChanged -= OnAnyPlaybackStateChanged;
            _mediaManager.OnFocusedSessionChanged -= OnFocusedSessionChanged;
            _mediaManager.Dispose();
            _mediaManager = null;
        }
        _isStarted = false;
    }

    private static void OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
    {
        MediaTrackChanged?.Invoke();
    }

    private static void OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
    {
        PlaybackStateChanged?.Invoke();
    }

    private static void OnFocusedSessionChanged(MediaManager.MediaSession sender)
    {
        // 焦点会话切换意味着"正在播放的程序"可能变了，按播放状态变化处理
        PlaybackStateChanged?.Invoke();
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
    /// 获取当前正在播放的歌曲（标题、歌手、来源程序、封面字节）
    /// </summary>
    public static async Task<MediaSong?> GetCurrentSongAsync()
    {
        var session = GetCurrentSession();
        if (session?.ControlSession == null)
            return null;

        try
        {
            var props = await session.ControlSession.TryGetMediaPropertiesAsync();
            if (props == null) return null;

            var appName = GetSourceAppName();
            if (appName != null && appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                appName = appName[..^4];

            return new MediaSong
            {
                Title = props.Title,
                Artist = props.Artist,
                AppName = string.IsNullOrWhiteSpace(appName) ? "未知程序" : appName,
                CoverBytes = await GetCoverBytesAsync(props)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 读取当前曲目的封面字节（后台线程调用安全）
    /// </summary>
    private static async Task<byte[]?> GetCoverBytesAsync(GlobalSystemMediaTransportControlsSessionMediaProperties props)
    {
        try
        {
            if (props.Thumbnail == null) return null;

            using var stream = (await props.Thumbnail.OpenReadAsync()).AsStreamForRead();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
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
            Console.WriteLine($@"获取源程序名称失败: {ex.Message}");
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
