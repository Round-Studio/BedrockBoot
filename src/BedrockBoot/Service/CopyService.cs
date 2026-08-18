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
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service;

public class CopyService
{
    // 用于内部传递探测结果的记录类型
    private record ClipboardResult(ClipboardContentType ContentType, object? Data = null);

    /// <summary>
    /// 设置剪切板文本（供外部调用，如复制 ID 到剪切板）
    /// </summary>
    public static async Task SetClipboard(string content, CopyType type, int Id)
    {
        var clipboard = GetClipboard();
        var typeStr = type switch
        {
            CopyType.Resource => "RC"
        };
        if (clipboard != null) 
            await clipboard.SetTextAsync($"{content}\nID: {typeStr}-{Id}");
    }

    /// <summary>
    /// 【统一调度入口】
    /// 识别剪切板类型并执行相应操作
    /// </summary>
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
                    var (id, type) = ((int, CopyType))result.Data!;
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

    /// <summary>
    /// 【类型探测类】
    /// 检查剪切板并返回对应的业务类型及预解析的数据
    /// </summary>
    private static async Task<ClipboardResult> GetClipboardContentType(IClipboard clipboard)
    {
        var formats = await clipboard.GetDataFormatsAsync();

        // 检查是否包含文件 (从资源管理器复制的文件)
        if (formats.Contains(DataFormat.File))
        {
            var files = await GetClipboardFiles(clipboard);
            if (files != null && files.Any())
            {
                return new ClipboardResult(ClipboardContentType.Files, files);
            }
        }

        // 检查是否包含符合自定义正则的文本
        if (formats.Contains(DataFormat.Text))
        {
            var text = await clipboard.TryGetTextAsync();
            var (id, type) = ParseIdAndType(text);
            if (id.HasValue && type != null)
            {
                return new ClipboardResult(ClipboardContentType.CustomText, (id.Value, type.Value));
            }
        }

        return new ClipboardResult(ClipboardContentType.None);
    }

    #region 具体的业务操作逻辑 (Action Handlers)

    /// <summary>
    /// 处理物理文件路径的逻辑
    /// </summary>
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

    /// <summary>
    /// 处理自定义 ID 文本的逻辑
    /// </summary>
    private static async Task HandleCustomTextAction(int id, CopyType type)
    {
        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = $"识别到 {type} 资源 ID: {id}"
        });

        if (type == CopyType.Resource)
        {
            await FetchCurseForgeInfo(id);
        }
    }

    #endregion

    #region 辅助底层工具 (Helper Methods)

    /// <summary>
    /// 获取物理剪切板中的文件路径列表
    /// </summary>
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

    /// <summary>
    /// 正则解析 ID 和 Type
    /// </summary>
    private static (int? Id, CopyType? Type) ParseIdAndType(string? text)
    {
        if (string.IsNullOrEmpty(text)) return (null, null);

        // 尝试匹配新格式：RC-123456
        var newMatch = Regex.Match(text, @"\b(RC|SC|DC?)-(\d+)\b", RegexOptions.IgnoreCase);
        if (newMatch.Success && int.TryParse(newMatch.Groups[2].Value, out var id))
        {
            var type = newMatch.Groups[1].Value.ToUpper() switch
            {
                "RC" => CopyType.Resource,
                // 可扩展其他类型
                _ => (CopyType?)null
            };
            return (id, type);
        }

        // 尝试匹配旧格式：ID: 123456 Type: Resource
        var oldMatch = Regex.Match(text, @"ID:\s*(\d+).*?Type:\s*(\S+)", RegexOptions.Singleline);
        if (oldMatch.Success && int.TryParse(oldMatch.Groups[1].Value, out var oldId))
        {
            var type = oldMatch.Groups[2].Value switch
            {
                "Resource" => CopyType.Resource,
                _ => (CopyType?)null
            };
            return (oldId, type);
        }

        return (null, null);
    }

    /// <summary>
    /// 获取 CurseForge 详细信息并打开下载界面
    /// </summary>
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

    /// <summary>
    /// 获取当前窗口的剪切板实例
    /// </summary>
    private static IClipboard? GetClipboard() => TopLevel.GetTopLevel(GlobalModel.MainWindow)?.Clipboard;

    #endregion
}