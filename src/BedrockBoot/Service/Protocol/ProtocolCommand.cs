using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.MainSubPage;
using BedrockBoot.Views.Pages.OtherPage;

namespace BedrockBoot.Service.Protocol;

public class ProtocolCommand
{
    public static void OnCommand(string[] command)
    {
        if (command.Length == 0)
            return;

        var raw = string.Join(" ", command);

        var protocolCommand = BedrockbootProtocolHandler.ParseProtocolUrl(raw);
        if (protocolCommand != null)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() => BedrockbootProtocolHandler.ExecuteCommandAsync(protocolCommand));
            return;
        }
    }
}