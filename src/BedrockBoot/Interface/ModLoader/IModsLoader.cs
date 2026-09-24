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
using BedrockBoot.Base.Entry.Game;

namespace BedrockBoot.Interface.ModLoader;

public interface IModsLoader
{
    public static virtual bool IsInstallModLoader(string path) => false;
    public string LoaderName { get; }
    public string LoaderDescription { get; }
    public bool CanRemove { get; }
    public bool IsAllowDisabling { get; }
    public string? IconUri { get; }
    public string ModsFolder { get; }
    public VersionConfig GameInstance { get; set; }
    public void InitLoader(VersionConfig instance);
    public void PreLaunch();
    public Task<bool> ApplicableInstance();
    public string GetInstalledVersion();
    public bool IsInstalled();
    public void Install();
    public void Remove();
    public void ViewInfo();
    public Action? OnUpdate { get; set; }
    public IModsManager ModsManager { get; set; }
    public bool GetIsEnabled();
    public void SetIsEnabled(bool isEnabled);
}