using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.DrawContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service;

public class CopyService
{
    public static async Task SetClipboard(string content, CopyType type, int Id)
    {
        var clipboard = TopLevel.GetTopLevel(GlobalModel.MainWindow)?.Clipboard;
        if (clipboard != null) await clipboard.SetTextAsync($"{content}\nID: {Id} Type: {type}");
    }

    public static (int? Id, CopyType? Type) ParseIdAndType(string text)
    {
        var pattern = @"ID:\s*(\d+).*?Type:\s*(\S+)";

        var match = Regex.Match(text, pattern, RegexOptions.Singleline);

        if (match.Success && match.Groups.Count >= 3)
        {
            var id = int.Parse(match.Groups[1].Value);
            var type = match.Groups[2].Value switch
            {
                "Resource" => CopyType.Resource
            };
            return (id, type);
        }

        return (null, null);
    }

    public static async Task HandleCopyAction()
    {
        var clipboard = TopLevel.GetTopLevel(GlobalModel.MainWindow)?.Clipboard;
        if (clipboard != null)
            try
            {
                var (id, type) = ParseIdAndType(await clipboard.TryGetTextAsync());
                GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
                {
                    Title = "剪切板",
                    Message = $"已获取剪切板，类型 {type}"
                });
                if (type == CopyType.Resource)
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Content = "正在等待 CurseForge 响应...",
                        Title = "获取模组信息"
                    });

                    try
                    {
                        var apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
                        var info = await apiClient.GetModDetailsAsync((int)id);

                        GlobalModel.MainWindow.OpenDraw(new DrawDownloadCurseForgeResourceContent(info),
                            $"下载资源 {info.Name}");
                    }
                    catch
                    {
                    }
                    finally
                    {
                        DialogHost.Close();
                    }
                }
            }
            catch
            {
            }
    }
}