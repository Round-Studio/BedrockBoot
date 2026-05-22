using Avalonia.Controls;
using Avalonia.Threading;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Helper;

public class GameServiceNotice
{
    public static void UnInstallGameService()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            DialogHost.Show(new DialogInfo()
            {
                Title = "未安装 GameService 组件",
                Content = new StackPanel()
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "当前系统未安装 GameService 组件\n" +
                                   "在未安装 GameService 组件的情况下，Minecraft 将无法启动。\n" +
                                   "请安装 GameService，随后再次启动游戏。"
                        },
                        new HyperlinkButton()
                        {
                            Content = "点击此处前往微软商店安装",
                            NavigateUri = new Uri("ms-windows-store://pdp/?productid=9MWPM2CQNLHN")
                        },
                        new HyperlinkButton()
                        {
                            Content = "点击此处前往微软商店 (网页端) 查看",
                            NavigateUri =
                                new Uri("https://apps.microsoft.com/detail/9mwpm2cqnlhn?hl=zh-cn&gl=CN&ocid=pdpshare")
                        },
                        new TextBlock()
                        {
                            Text = "详情参见："
                        },
                        new HyperlinkButton()
                        {
                            Content = "常见问题 - BedrockBoot 文档",
                            NavigateUri = new Uri("https://docs.roundstudio.top/docs/product/bb/commonQuestion")
                        }
                    }
                },
                AccountButton = DialogButtons.CloseButton,
                CloseButtonText = "确定"
            });
        });
    }
}