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
using BedrockBoot.Models.Global;
using Microsoft.Win32;

namespace BedrockBoot.Models.Helper;

public class OpenAgreement
{
    public static void RegisterAssociation()
    {
        var exePath = Environment.ProcessPath;
        var ResProgId = "BedrockBoot.Win32";
        var WorldProgId = "BedrockBoot.Desktop";
        var TemplateProgId = "BedrockBoot.Template";

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcpack"))
        {
            extKey.SetValue("", ResProgId);
        }

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcaddon"))
        {
            extKey.SetValue("", ResProgId);
        }

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mcworld"))
        {
            extKey.SetValue("", WorldProgId);
        }

        using (var extKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mctemplate"))
        {
            extKey.SetValue("", TemplateProgId);
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ResProgId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 附加包文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{exePath}\",{SourceList.PackIconID}");
            }

            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{exePath}\" -open --resource \"%1\"");
            }
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{WorldProgId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 世界文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{exePath}\",{SourceList.PackIconID}");
            }

            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{exePath}\" -open --world \"%1\"");
            }
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{TemplateProgId}"))
        {
            progIdKey.SetValue("", "Minecraft Bedrock 世界模板文件");
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"\"{exePath}\",{SourceList.PackIconID}");
            }

            using (var cmdKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                cmdKey.SetValue("", $"\"{exePath}\" -open --template \"%1\"");
            }
        }
    }
}