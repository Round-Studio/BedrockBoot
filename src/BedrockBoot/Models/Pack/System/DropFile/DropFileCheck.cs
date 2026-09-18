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

using System.IO;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileCheck
{
    /// <summary>
    /// 获取对应类型文件的名字
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static string GetFileTypeName(string file) => GlobalKeys.DropOverTypesOfSupport[Path.GetExtension(file)].Name;
    public static SupportedFileType GetFileType(string file) => GlobalKeys.DropOverTypesOfSupport[Path.GetExtension(file)].Type;
}