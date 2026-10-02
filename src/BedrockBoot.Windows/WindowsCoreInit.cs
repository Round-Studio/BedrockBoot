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

using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Standard.Interface.Platform;
using BedrockBoot.Windows.Models.Game;
using BedrockBoot.Windows.Models.Global;
using BedrockBoot.Windows.Models.Helper;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace BedrockBoot.Windows;

public class WindowsCoreInit : ICoreInit
{
    public void UpdateUseHardwareDecode(bool isUse)
    {
        Console.WriteLine($@"使用硬件解码：{isUse}");
        IsUseHardwareDecode = isUse;
    }

    public bool IsUseHardwareDecode { get; set; }

    public async Task InitializeAsync()
    {
        Round.SDK.Plugin.BedrockBoot.Register.RegisterService.RegisterLaunchingEvent((s =>
        {
            var gameInfo = GameInfoHelper.GetVersionConfig(s);
            var bodyFile = Path.Combine(gameInfo.VersionPath!, gameInfo.BodyFile!);
            var isAdmin = FileCompatibilityChecker.IsRunAsAdminChecked(bodyFile);
            Console.WriteLine($@"当前游戏文件是否需要管理员运行：{isAdmin}");
            if (isAdmin)
            {
                FileCompatibilityChecker.RemoveRunAsAdmin(bodyFile);
                Console.WriteLine(@"已取消文件的管理员权限");
            }
        }));
        
        CoreGlobal.BedrockCore = new BedrockCore
        {
            Options = new CoreOptions
            {
                IsAutoCompleteVC = false,
                IsAutoOpenDevelopment = false,
                IsAutoCompleteGameInput = false,
                IsCheckMD5 = true
            }
        };
        await CoreGlobal.BedrockCore.InitAsync();
    }
}