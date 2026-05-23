using BedrockBoot.Models.Helper;

namespace BedrockBoot.Models.Media;

public class MediaHelper
{
    /// <summary>
    /// 获取当前正在播放媒体的程序名称
    /// </summary>
    public static string? GetPlayingAppName()
    {
#if WINDOWS
        if (!SMTCHelper.IsStarted)
        {
            SMTCHelper.Initialize();
        }

        return SMTCHelper.GetSourceAppName();
#else
        Console.WriteLine(@"非 Windows 平台无法获取系统播放状态");
        return null;
#endif
    }

    /// <summary>
    /// 判断系统当前是否有媒体正在播放
    /// </summary>
    public static bool IsNowPlaying()
    {
#if WINDOWS
        if (!SMTCHelper.IsStarted)
        {
            SMTCHelper.Initialize();
        }

        return SMTCHelper.IsPlaying();
#else
        Console.WriteLine(@"非 Windows 平台无法获取系统播放状态");
        return false;
#endif
    }
}