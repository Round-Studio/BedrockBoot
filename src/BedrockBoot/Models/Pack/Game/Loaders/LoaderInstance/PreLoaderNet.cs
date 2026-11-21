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
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using BedrockBoot.Standard.Entity.Game;
using BedrockBoot.Models.Pack.Game.Loaders.ModsManagers;
using BedrockBoot.Standard.Interface.ModLoader;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Views.DialogContent.Loader;

namespace BedrockBoot.Models.Pack.Game.Loaders.LoaderInstance;

public class PreLoaderNet : IModsLoader
{
    public Action? OnUpdate { get; set; }
    public IModsManager ModsManager { get; set; }
    public bool GetIsEnabled() => true;

    public void SetIsEnabled(bool isEnabled)
    {
    }

    public static bool IsInstallModLoader(string path) => true;
    public string LoaderName => I18nManager.Instance["Pack.Loader.PreLoader.Name"];
    public string LoaderDescription => I18nManager.Instance["Pack.Loader.PreLoader.Desc"];
    public string? LoaderWebsite { get; } = "https://github.com/Round-Studio/BedrockBoot";
    public string LoaderIntro => I18nManager.Instance["Pack.Loader.PreLoader.Intro"];
    public bool CanRemove { get; } = false;
    public bool IsAllowDisabling { get; } = false;
    public string? IconUri { get; } = "avares://BedrockBoot/Assets/Icon/BedrockBoot.Icon.256x.png";
    public string ModsFolder => Path.Combine(GameInstance.VersionPath!, "config", "BedrockBoot2", "mods");
    public VersionConfig GameInstance { get; set; }

    public void InitLoader(VersionConfig instance)
    {
        GameInstance = instance;
        ModsManager = new PreLoaderModsManager() { OnRefresh = OnUpdate };
        ModsManager.Init(GameInstance);
    }

    public void PreLaunch()
    {
    }

    public async Task<bool> ApplicableInstance() => true;

    public string GetInstalledVersion()
    {
        if (PreLoadDllStore.HasUpdatedDll())
            try
            {
                File.WriteAllBytes(Path.Combine(GameInstance.VersionPath!, "PreLoad.NET.dll"),
                    PreLoadDllStore.GetDllBytes());

                Console.WriteLine(@"PreLoad.NET.dll 释放完毕");
            }
            catch
            {
            }

        return GetAllInstalledVersion();
    }

    public bool IsInstalled() => true;

    public void Install()
    {
        throw new Exception(I18nManager.Instance["Pack.Loader.PreLoader.CannotInstall"]);
    }

    public void Remove()
    {
        throw new Exception(I18nManager.Instance["Pack.Loader.PreLoader.CannotRemove"]);
    }

    public void ViewInfo()
    {
        DialogLoaderViewContent.Show(this);
    }

    private string? GetAllInstalledVersion()
    {
        try
        {
            string dllPath = Path.Combine(GameInstance.VersionPath!, "PreLoad.NET.dll");

            if (!File.Exists(dllPath))
            {
                Console.WriteLine(@"DLL 文件不存在");
                return null;
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
            string version = versionInfo.FileVersion ?? versionInfo.ProductVersion ?? null;

            Console.WriteLine(@$"Version: {version}");
            return version;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"读取版本失败: {ex.Message}");
            return null;
        }
    }
}