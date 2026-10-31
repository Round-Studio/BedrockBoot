using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Helper;
using BedrockBoot.Windows.Entity;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawGameCrashContent : UserControl
{
    private readonly GameCrashReportEventArgs _args;

    public DrawGameCrashContent()
    {
        InitializeComponent();
        SystemName.Text = $"{Environment.OSVersion.VersionString}";
        SystemArch.Text = RuntimeInformation.OSArchitecture.ToString();
    }

    public DrawGameCrashContent(GameCrashReportEventArgs args) : this()
    {
        _args = args;

        var conf = GameInfoHelper.GetVersionConfig(args.InstanceFolder);
        GameCorePath.Text = Path.Combine(conf.VersionPath, conf.BodyFile);
        GameVersion.Text = conf.Info.Version;
        GameInstance.Text = _args.GameName;
        DumpFilePath.Text = _args.DumpFilePath;
        SourceFile.Text = _args.Report?.Owner;
        ExitCode.Text = _args.ExitCode.ToString();
        Stack.Text = string.Join(Environment.NewLine,
            _args.Report?.StackTrace.Select(x =>
            {
                var head = $"0x{x.Address:X}";
                if (string.IsNullOrEmpty(x.ModulePath))
                    return head;
                if (string.IsNullOrEmpty(x.SymbolName))
                    return $"{head} {x.ModulePath}";
                var disp = x.SymbolDisplacement.HasValue ? $"+0x{x.SymbolDisplacement.Value:X}" : "";
                return $"{head} {x.ModulePath} | {x.SymbolName}{disp}";
            }));
    }
}