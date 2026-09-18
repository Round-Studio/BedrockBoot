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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service;

public class CopyService
{
    private record ClipboardResult(ClipboardContentType ContentType, object? Data = null);

    private static readonly Dictionary<string, SearchResourceType> PrefixToType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RC"] = SearchResourceType.ResourcePack,
        ["DM"] = SearchResourceType.DllMods,
        ["LI"] = SearchResourceType.LeviLaminaMods,
        ["PL"] = SearchResourceType.PluginPack,
    };

    private static readonly Dictionary<SearchResourceType, string> TypeToPrefix =
        PrefixToType.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static async Task SetClipboard(string shareContent, SearchResultItemInfo searchResultItemInfo)
    {
        var clipboard = GetClipboard();
        if (clipboard == null) return;

        if (!TypeToPrefix.TryGetValue(searchResultItemInfo.ResourceType, out var typeStr))
            return;

        await clipboard.SetTextAsync($"{shareContent}\nID: {typeStr}-{searchResultItemInfo.Id}");
    }

    public static async Task HandleCopyAction()
    {
        var clipboard = GetClipboard();
        if (clipboard == null) return;

        try
        {
            var result = await GetClipboardContentType(clipboard);

            Console.WriteLine($@"读取剪切板：{result.ContentType}");

            switch (result.ContentType)
            {
                case ClipboardContentType.Files:
                    await HandleFilesAction((IEnumerable<string>)result.Data!);
                    break;

                case ClipboardContentType.CustomText:
                    var (id, type) = ((string, SearchResourceType))result.Data!;
                    await HandleCustomTextAction(id, type);
                    break;

                case ClipboardContentType.None:
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"调度异常: {ex.Message}");
        }
    }

    private static async Task<ClipboardResult> GetClipboardContentType(IClipboard clipboard)
    {
        var formats = await clipboard.GetDataFormatsAsync();

        if (formats.Contains(DataFormat.File))
        {
            var files = await GetClipboardFiles(clipboard);
            if (files != null && files.Any())
            {
                return new ClipboardResult(ClipboardContentType.Files, files);
            }
        }

        if (formats.Contains(DataFormat.Text))
        {
            var text = await clipboard.TryGetTextAsync();
            var (id, type) = ParseIdAndType(text);
            if (id != null && type.HasValue)
            {
                return new ClipboardResult(ClipboardContentType.CustomText, (id, type.Value));
            }
        }

        return new ClipboardResult(ClipboardContentType.None);
    }

    #region 具体的业务操作逻辑 (Action Handlers)

    private static async Task HandleFilesAction(IEnumerable<string> files)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0) return;

        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = $"成功读取 {fileList.Count} 个文件，准备导入..."
        });

        await Task.CompletedTask;
    }

    private static async Task HandleCustomTextAction(string id, SearchResourceType type)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = $"识别到 {type} 资源 ID: {id}"
        });

        switch (type)
        {
            case SearchResourceType.ResourcePack:
                if (int.TryParse(id, out var numericId))
                {
                    await FetchCurseForgeInfo(numericId);
                }
                else
                {
                    GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                    {
                        Title = "剪切板",
                        Message = $"无法解析 CurseForge 数字 ID: {id}"
                    });
                }

                break;

            case SearchResourceType.DllMods:
            case SearchResourceType.LeviLaminaMods:
            case SearchResourceType.PluginPack:
                break;
        }
    }

    #endregion

    #region 辅助底层工具 (Helper Methods)

    private static async Task<IEnumerable<string>?> GetClipboardFiles(IClipboard clipboard)
    {
        var data = await clipboard.TryGetDataAsync();
        if (data == null) return null;

        var paths = new List<string>();
        foreach (var item in data.Items)
        {
            if (item.Formats.Contains(DataFormat.File))
            {
                var fileData = await item.TryGetRawAsync(DataFormat.File);
                if (fileData is IStorageFile storageFile)
                    paths.Add(storageFile.Path.LocalPath);
            }
        }

        return paths.Count > 0 ? paths : null;
    }

    private static (string? Id, SearchResourceType? Type) ParseIdAndType(string? text)
    {
        if (string.IsNullOrEmpty(text)) return (null, null);

        var match = Regex.Match(
            text,
            @"^\s*ID:\s*(RC|DM|LI|PL)-(\S+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        if (!match.Success)
            return (null, null);

        var id = match.Groups[2].Value;
        if (string.IsNullOrEmpty(id))
            return (null, null);

        if (!PrefixToType.TryGetValue(match.Groups[1].Value, out var type))
            return (null, null);

        return (id, type);
    }

    private static async Task FetchCurseForgeInfo(int id)
    {
        DialogHost.Show(new DialogInfo { Content = "正在等待 CurseForge 响应...", Title = "获取模组信息" });
        try
        {
            var apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
            var info = await apiClient.GetModDetailsAsync(id);
            GlobalModel.MainWindow.OpenDraw(new DrawDownloadCurseForgeResourceContent(info), $"下载资源 {info.Name}");
        }
        catch (Exception ex)
        {
            GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo { Title = "获取失败", Message = ex.Message });
        }
        finally
        {
            DialogHost.Close();
        }
    }

    private static IClipboard? GetClipboard() => TopLevel.GetTopLevel(GlobalModel.MainWindow)?.Clipboard;

    #endregion
}