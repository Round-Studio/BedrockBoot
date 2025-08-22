using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Tools
{
    internal class GetANotification
    {
        public AppNotification GetNotification(List<string> notificationsText)
        {
            AppNotificationBuilder notification = new AppNotificationBuilder();
            foreach (var item in notificationsText)
            {
                notification.AddText(item);
            }
            return notification.BuildNotification();
        }

        public AppNotification GetNotification(List<string> notificationsText, AppNotificationProgressData data)
        {
            /// <summary>
            /// 使用进度条的通知，使用较为复杂，导航至原定义查看文档
            /// </summary>
            ///
            /// Doc: https://learn.microsoft.com/zh-cn/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications.builder.appnotificationprogressbar
            AppNotificationBuilder notification = new AppNotificationBuilder();
            foreach (var item in notificationsText)
            {
                notification.AddText(item);
            }

            notification.AddProgressBar(new AppNotificationProgressBar()
                .BindTitle()
                .BindStatus()
                .BindValue()
                .BindValueStringOverride()
                );
            return notification.BuildNotification();
        }
    }
}
