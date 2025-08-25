using Microsoft.Windows.BadgeNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace BedrockBoot.Tools
{
    /* 这里有所有的徽章类型
    public enum BadgeNotificationTypes
    {
        Activity = BadgeNotificationGlyph.Activity,
        Alert = BadgeNotificationGlyph.Alert,
        Alarm = BadgeNotificationGlyph.Alarm,
        Attention = BadgeNotificationGlyph.Attention,
        Available = BadgeNotificationGlyph.Available,
        Away = BadgeNotificationGlyph.Away,
        Busy = BadgeNotificationGlyph.Busy,
        Error = BadgeNotificationGlyph.Error,
        NewMessage = BadgeNotificationGlyph.NewMessage,
        Paused = BadgeNotificationGlyph.Paused,
        Playing = BadgeNotificationGlyph.Playing,
        Unavailable = BadgeNotificationGlyph.Unavailable,
    }
    */
    internal class EasyBadgeNotification
    {
        public static void ClearBadgeNotification()
        {
            BadgeNotificationManager.Current.ClearBadge();
        }

        public static void SetBadgeNotification(BadgeNotificationGlyph types)
        {
            BadgeNotificationManager.Current.SetBadgeAsGlyph(types);
        }
        public static void SetBadgeNotification(uint Value)
        {
            BadgeNotificationManager.Current.SetBadgeAsCount(Value);
        }

        public static void UpdateBadgeNotification(BadgeNotificationGlyph types)
        {
            ClearBadgeNotification();
            SetBadgeNotification(types);
        }

        public static void UpdateBadgeNotification(uint Value)
        {
            ClearBadgeNotification();
            SetBadgeNotification(Value);
        }
    }
}
