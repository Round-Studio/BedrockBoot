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
using System.Collections.Generic;
using BedrockBoot.Core;
using BedrockBoot.Localization;
using BedrockBoot.Standard.Enum.Type;

namespace BedrockBoot.Models.Global;

public class GlobalKeys
{
    public const string MsClientId = "0000000048183522";

    public static string CurseForgeApiKey { get; } = Keys.CurseForgeApiKey;

    public static Dictionary<string, (SupportedFileType Type, bool AllowMany, string NameKey)> DropOverTypesOfSupport { get; } = new()
    {
        { ".mcpack", (SupportedFileType.Mcpack, true, "Core.FileType.Mcpack") },
        { ".mcaddon", (SupportedFileType.Mcaddon, true, "Core.FileType.Mcaddon") },
        { ".mcworld", (SupportedFileType.Mcworld, true, "Core.FileType.Mcworld") },
        { ".mctemplate", (SupportedFileType.Mctemplate, false, "Core.FileType.Mctemplate") },
        /*{ ".mcpint", (SupportedFileType.Mcpint, false, "Core.FileType.Mcpint") },*/
        { ".rskin", (SupportedFileType.Rskin, true, "Core.FileType.Rskin") },
        { ".rplck", (SupportedFileType.Rplck, true, "Core.FileType.Rplck") },
        /*{ ".dll", (SupportedFileType.Dll, false, "Core.FileType.Dll") },*/ // 这个还没想好逻辑，等以后想好再加
        /*{ ".zip", (SupportedFileType.Zip,true, "Core.FileType.Zip") },
        { ".appx", (SupportedFileType.Appx, false, "Core.FileType.Appx") },
        { ".msixvc", (SupportedFileType.Msixvc, false, "Core.FileType.Msixvc") }*/
    };

    /// <summary>
    ///     取某个扩展名的可读类型名（已本地化）。不支持的扩展名原样返回扩展名。
    /// </summary>
    public static string GetFileTypeDisplayName(string extension)
    {
        if (string.IsNullOrEmpty(extension) ||
            !DropOverTypesOfSupport.TryGetValue(extension, out var info))
            return extension;

        var name = Localizer.Get(info.NameKey);
        return string.IsNullOrEmpty(name) ? info.NameKey : name;
    }
    
    public static string DropOverTypesOfSupportString => string.Join(", ", DropOverTypesOfSupport.Keys);
}