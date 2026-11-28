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
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;
using BedrockBoot.Models.Pack.LeviLamina;

namespace BedrockBoot.Models.Pack.LeviLamina;

public class LeviLaminaModsInstaller
{
    private readonly LeviModEntry _entry;
    private readonly LeviModVersion _version;
    private readonly string _label;

    public LeviLaminaModsInstaller(LeviModEntry entry, LeviModVersion version, string label)
    {
        _entry = entry;
        _version = version;
        _label = label;
    }

    public async Task Install(string savePath)
    {
        var dialogContent = new DialogInstallLeviLaminaModContent(BuildInstallString(), savePath, _version?.ManifestUrl);

        DialogHost.Show(new DialogInfo
        {
            Title = string.Format(I18nManager.Instance["Pack.LeviLamina.InstallMod.Title"],
                _entry?.Name ?? _entry?.Id ?? "", _version?.Version ?? ""),
            Content = dialogContent
        });
    }

    // 变体标签即 tooth 标签：client / client_lua / client_quickjs…；空标签（如 LeviLamina 本体）不带 # 后缀
    private string BuildInstallString()
    {
        var id = _entry?.Id ?? "";
        var version = _version?.Version ?? "";

        return string.IsNullOrEmpty(_label)
            ? $"{id}@{version}"
            : $"{id}#{_label}@{version}";
    }
}
