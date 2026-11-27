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

using BedrockBoot.Standard.Entity.Game;

namespace BedrockBoot.Standard.Interface.ModLoader;

public interface IModsLoader
{
    public static virtual bool IsInstallModLoader(string path) => false;
    public string LoaderName { get; }
    public string LoaderDescription { get; }
    public string? LoaderWebsite => null;
    public string LoaderIntro => string.Empty;
    public bool CanRemove { get; }
    public bool IsAllowDisabling { get; }
    public string? IconUri { get; }
    public string ModsFolder { get; }
    public List<AddonEvent> AddonEvents { get; set; }
    public VersionConfig GameInstance { get; set; }
    public void InitLoader(VersionConfig instance);
    public void PreLaunch();
    public Task<bool> ApplicableInstance();
    public string GetInstalledVersion();
    public bool IsInstalled();
    public void Install();
    public void Remove();
    public void ViewInfo();
    /// <summary>是否支持升级加载器。默认不支持，由加载器按需覆写。</summary>
    public bool CanUpgrade => false;
    /// <summary>可升级到的目标版本；null 表示当前无可升级。</summary>
    public Task<string?> GetUpgradeTargetAsync() => Task.FromResult<string?>(null);
    /// <summary>执行升级。由 CanUpgrade 与目标版本门控后由 UI 调用。</summary>
    public void Upgrade()
    {
    }
    public Action? OnUpdate { get; set; }
    public IModsManager ModsManager { get; set; }
    public bool GetIsEnabled();
    public void SetIsEnabled(bool isEnabled);
}

public class AddonEvent
{
    public string TagHeader { get; set; } = string.Empty;
    public string TagDescription { get; set; } = string.Empty;
    public string Glyph { get; set; } = string.Empty;
    public Action<VersionConfig>? EventCallBack { get; set; }
}