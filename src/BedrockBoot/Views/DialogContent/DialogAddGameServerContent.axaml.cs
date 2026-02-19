using System;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Game.Pack.Server;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.DialogContent;

public partial class DialogAddGameServerContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;

    public DialogAddGameServerContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 获取根据用户输入生成的服务器信息对象
    /// </summary>
    public ServerItemInfo ServerItemInfo
    {
        get
        {
            // 尝试解析端口，如果失败或为空则使用基岩版默认端口 19132
            if (!int.TryParse(ServerPortInputBox.Text, out var port))
            {
                port = 19132;
            }

            return new ServerItemInfo
            {
                // 如果名称为空，使用国际化后的默认名称
                ServerName = !string.IsNullOrWhiteSpace(ServerNameInputBox.Text) 
                    ? ServerNameInputBox.Text 
                    : i18n["Dialog.AddServer.DefaultName"],
                
                // 地址通常是必填项，此处保持原始引用
                ServerAddress = ServerAddressInputBox.Text ?? string.Empty,
                
                ServerPort = port,
                VersionConfig = null
            };
        }
    }
}