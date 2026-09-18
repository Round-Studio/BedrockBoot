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
using System.Threading.Tasks;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Models.Pack.LeviLamina;

public class LeviLaminaModsInstaller
{
    private readonly PackageInfo _pkg;
    private readonly string _key;

    public LeviLaminaModsInstaller(PackageInfo pkg, string key)
    {
        _pkg = pkg;
        _key = key;
    }

    public async Task Install(string versionId, string savePath, bool isOnlyDownload = false)
    {
        if (!isOnlyDownload)
        {
            var dialogContent = new DialogInstallLeviLaminaModContent(versionId, savePath, _key, isOnlyDownload);

            DialogHost.Show(new DialogInfo
            {
                Title = $"正在安装 LeviLamina v{versionId}",
                Content = dialogContent
            });
        }
    }
}