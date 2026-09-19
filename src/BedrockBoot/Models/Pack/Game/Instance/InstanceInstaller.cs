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

using BedrockBoot.Base.Entry;
using BedrockBoot.Views.DialogContent.Install;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Models.Pack.Game.Instance;

public class InstanceInstaller
{
    private readonly BuildInfo _info;

    public InstanceInstaller(BuildInfo info)
    {
        _info = info;
    }

    public void Install()
    {
        var dialog = new DialogGameInstallInfoContent(_info);
        DialogHost.Show(new DialogInfo()
        {
            Content = dialog,
            Title = "安装实例",
            CloseButtonText = "安装",
            PrimaryButtonText = "取消",
            CloseAction = () => dialog.ExecuteInstallTask()
        });
    }
}