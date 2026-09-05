using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Entry.Info.Download;
using BedrockBoot.Interface.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Pack.Game.ResourcePack.CurseForge;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Service.Download;

public class CurseForgeDownloadResult : IDownloadResult
{
    public CurseForgeDownloadResult(SearchResultItemInfo searchResultItemInfo)
    {
        SearchInfo = searchResultItemInfo;
    }

    public SearchResultItemInfo SearchInfo { get; set; }
    private static I18nManager i18n => I18nManager.Instance;
    public bool IsHasManyFiles { get; } = true;

    public async Task<List<Control>?> DescriptionControls()
    {
        var apiClient = new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey);
        var descriptionHtml = await apiClient.GetModDescriptionAsync(SearchInfo.Id);

        if (!string.IsNullOrEmpty(descriptionHtml))
        {
            var controls = HtmlToControlConverter.ConvertHtmlToControls(descriptionHtml);
            return controls;
        }

        return null;
    }

    public async Task<uint> GetDownloadCount() => SearchInfo.DownloadCount;
    public async Task<bool> IsInstalled() => false;
    public Task Install() => throw new System.NotImplementedException();
    public Task ReInstall() => throw new System.NotImplementedException();
    public void Delete() => throw new System.NotImplementedException();

    public async Task<List<ResourceFileInfo>> GetFiles()
    {
        var files = await new CurseForgeApiClient(GlobalKeys.CurseForgeApiKey)
            .GetModFilesAsync(SearchInfo.Id);

        return files.Data.Select(x => new ResourceFileInfo()
        {
            FileName = x.FileName,
            Description = x.FileDate.ToString("yyyy-MM-dd HH:mm:ss"),
            Version = string.Join('.', x.GameVersions),
            FileSize = (uint)x.FileLength,
            IsEnableSaveAs = true,
            OnDownload = (s) =>
            {
                var dialog = new DialogChooseGameContent();

                DialogHost.Show(new DialogInfo
                {
                    Content = dialog,
                    Title = i18n["Download.CurseForge.InstallTo.Title"],
                    CloseButtonText = "下载",
                    SecondaryButtonText = i18n["MainWindow.Common.Cancel"],
                    CloseAction = () =>
                    {
                        var conf = dialog.VersionConfig;
                        if (conf == null) return;

                        GlobalModel.MainWindow.CloseDraw();

                        var tempFilePath = Path.Combine(PathsList.TempPath, x.FileName);
                        TaskDownloadCurseForgeResourceItem.Download(x, tempFilePath, conf);
                    },
                });
            },
            OnSaveAs = async (s) =>
            {
                var topLevel = TopLevel.GetTopLevel(GlobalModel.MainWindow);
                if (topLevel == null) return;

                var extension = Path.GetExtension(x.FileName);
                if (string.IsNullOrEmpty(extension)) extension = ".mcpack";

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = i18n["Download.CurseForge.SaveAs.Title"],
                    SuggestedFileName = x.FileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(i18n["Download.CurseForge.FileType.Bedrock"])
                        {
                            Patterns = new[] { $"*{extension}" }
                        }
                    }
                });

                var localPath = file?.TryGetLocalPath();
                if (!string.IsNullOrEmpty(localPath))
                {
                    GlobalModel.MainWindow.CloseDraw();
                    TaskDownloadCurseForgeResourceItem.Download(x, localPath);
                }
            }
        }).ToList();
    }
}