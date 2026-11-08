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
using Avalonia.Controls.Shapes;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Standard.Interface.ModLoader;
using BedrockBoot.Standard.Interface.Mods;
using BedrockBoot.Views.Pages.InstanceSubPage.DrawContent.ModSupport;
using Path = System.IO.Path;

namespace BedrockBoot.Models.Pack.DllMods.LeviLamina.Arke;

public class ArkeSupport : IModSupport
{
    public string ModName { get; } = "Arke";
    public string Tag => "LeviLamina.Arke";
    public string SupportName { get; } = "Arke 光影包";
    public string ModType { get; } = "LeviLamina";
    public object? SupportPage => new LArkeShaderPacksManager(Instance);
    public string Glyph { get; } = "\uF08C";
    public VersionConfig Instance { get; set; }

    public ArkeSupport(VersionConfig instance)
    {
        Instance = instance;
    }

    public bool IsEnable()
    {
        var path = Path.Combine(Instance.VersionPath, "config", "BedrockBoot2", "levilamina", "ll.mods", "arke",
            "arke.dll");
        return File.Exists(path);
    }
}

public class ArkeSupportFactory : IModSupportFactory
{
    public IModSupport Create(VersionConfig instance) => new ArkeSupport(instance);
}