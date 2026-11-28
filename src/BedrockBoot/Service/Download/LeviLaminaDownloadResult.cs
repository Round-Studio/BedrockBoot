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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Entity.Info.Download;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.LeviLamina;
using BedrockBoot.Standard.Interface.Download;
using BedrockBoot.Views.DialogContent.Loader.LeviLamina;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Download;

public class LeviLaminaDownloadResult : IDownloadResult
{
    private readonly LeviModEntry _entry;

    public LeviLaminaDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;

        try
        {
            _entry = JsonSerializer.Deserialize<LeviModEntry>(searchResultItemInfo.JsonData);
        }
        catch
        {
            _entry = null;
        }
    }

    public SearchResultItemInfo SearchInfo { get; set; }
    public bool IsHasManyFiles { get; } = true;

    public async Task<string?> GetDescriptionHtml()
    {
        var detail = await GetDetailAsync();
        var readme = PickReadme(detail?.Readmes);
        return readme?.Text;
    }

    public async Task<List<Control>?> DescriptionControls()
    {
        var html = await GetDescriptionHtml();
        return HtmlToControlConverter.ConvertHtmlToControls(html);
    }

    // 索引里自带全量下载计数，无需再请求 GitHub Releases
    public Task<uint> GetDownloadCount()
    {
        return Task.FromResult((uint)Math.Max(0, _entry?.Downloads ?? 0));
    }

    public Task<bool> IsInstalled() => Task.FromResult(false);

    public Task Install() => throw new System.NotImplementedException();
    public Task ReInstall() => throw new System.NotImplementedException();
    public void Delete() => throw new System.NotImplementedException();

    public async Task<List<ResourceFileInfo>> GetFiles()
    {
        var detail = await GetDetailAsync();

        var files = new List<ResourceFileInfo>();
        foreach (var version in detail?.Versions ?? new List<LeviModVersion>())
        {
            // 只列出带客户端变体的版本；变体标签即 tooth 标签（client / client_lua / 空标签…）
            var label = LeviModsSource.SelectClientVariantLabel(version);
            if (label == null) continue;

            var asset = LeviModsSource.GetVariantAsset(version, label);

            // 按「Minecraft x.y.z / LeviLamina a.b.c」分组：LL 约束解析成具体版本，MC 版本取自该 LL 版本的 bds 依赖
            var llSpec = LeviModsSource.GetLeviLaminaSpec(asset);
            var (llVersion, mcVersion) = await LeviModsSource.ResolveTargetVersionAsync(llSpec);

            string groupKey = null;
            string groupTitle = null;
            string groupSubtitle = null;
            if (!string.IsNullOrEmpty(llVersion))
            {
                groupSubtitle = $"LeviLamina {llVersion}";
                groupTitle = string.IsNullOrEmpty(mcVersion) ? groupSubtitle : $"Minecraft {mcVersion}";
                groupKey = $"{groupTitle}|{groupSubtitle}";
            }
            else
            {
                // 无 LL 依赖声明的远古版本归入兜底分组（ResultFiles 对该键默认折叠）
                groupTitle = I18nManager.Instance["Download.Result.Group.Unknown"];
                groupSubtitle = groupTitle;
                groupKey = "unknown";
            }

            files.Add(new ResourceFileInfo()
            {
                FileName = version.Version,
                Description = groupSubtitle,
                VersionGroup = groupKey,
                GroupKey = groupKey,
                GroupTitle = groupTitle,
                GroupSubtitle = groupSubtitle,
                IsPrerelease = version.Prerelease,
                OnDownload = (s) =>
                {
                    OpenInstallDialog(version, label);
                }
            });
        }

        return files;
    }

    private void OpenInstallDialog(LeviModVersion version, string label)
    {
        var chooseInstanceDialog = new DialogChooseLeviLaminaModInstallInstanceContent(_entry, version, label);
        DialogHost.Show(new()
        {
            Title = string.Format(I18nManager.Instance["LeviLamina.Search.Install"], SearchInfo.Name, version.Version),
            Content = chooseInstanceDialog,
            CloseButtonText = I18nManager.Instance["Shared.Action.Confirm"],
            PrimaryButtonText = I18nManager.Instance["Shared.Action.Cancel"],
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                var installer = new LeviLaminaModsInstaller(_entry, version, label);
                installer.Install(chooseInstanceDialog.SavePath);
            }
        });
    }

    private Task<LeviModDetail> GetDetailAsync()
    {
        var modId = _entry?.Id ?? SearchInfo?.Id;
        return LeviModsSource.GetModDetailAsync(modId);
    }

    // readme 多语言：优先当前界面语言，其次英文，最后取第一个
    private static LeviReadme PickReadme(List<LeviReadme> readmes)
    {
        if (readmes == null || readmes.Count == 0) return null;

        var language = BedrockBoot.Core.Global.GlobalModel.Config.Data.Language;
        var prefix = (language ?? string.Empty).Split('-')[0].ToLowerInvariant();

        return readmes.FirstOrDefault(r => string.Equals(r.Lang, language, StringComparison.OrdinalIgnoreCase))
               ?? readmes.FirstOrDefault(r => r.Lang != null && r.Lang.ToLowerInvariant().StartsWith(prefix))
               ?? readmes.FirstOrDefault(r => string.Equals(r.Lang, "en", StringComparison.OrdinalIgnoreCase))
               ?? readmes[0];
    }
}
