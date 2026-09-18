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
using BedrockBoot.Base.Enum.Type;

namespace BedrockBoot.Models.Global;

public class GlobalKeys
{
    public const string MsClientId = "0000000048183522";

    public static string CurseForgeApiKey { get; } =
        "$2a$10$Awb53b9gSOIJJkdV3Zrgp.CyFP.dI13QKbWn/4UZI4G4ff18WneB6";

    public static Dictionary<string, (SupportedFileType Type, bool AllowMany, string Name)> DropOverTypesOfSupport { get; } = new()
    {
        { ".mcpack", (SupportedFileType.Mcpack, true, "基岩版资源包") },
        { ".mcaddon", (SupportedFileType.Mcaddon, true, "基岩版集合包") },
        { ".mcworld", (SupportedFileType.Mcworld, true, "基岩版存档包") },
        { ".mctemplate", (SupportedFileType.Mctemplate, false, "基岩版世界模版") },
        /*{ ".mcpint", (SupportedFileType.Mcpint, false, "BedrockBoot 整合包文件") },*/
        { ".rskin", (SupportedFileType.Rskin, true, "Round-Studio 通用皮肤包") },
        { ".rplck", (SupportedFileType.Rplck, true, "Round-Studio 通用插件包") },
        /*{ ".dll", (SupportedFileType.Dll, false, "DLL 文件") },*/ // 这个还没想好逻辑，等以后想好再加
        /*{ ".zip", (SupportedFileType.Zip,true, "ZIP 文件") },
        { ".appx", (SupportedFileType.Appx, false, "Microsoft 安装包文件 (APPX)") },
        { ".msixvc", (SupportedFileType.Msixvc, false, "Microsoft 安装包文件 (MSIXVC)") }*/
    };
    
    public static string DropOverTypesOfSupportString => string.Join(", ", DropOverTypesOfSupport.Keys);
}