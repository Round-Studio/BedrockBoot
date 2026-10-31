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