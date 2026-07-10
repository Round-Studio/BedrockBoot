using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.Base.Entry.Game.Pack.Archive.Export;
using BedrockBoot.Base.Enum.Type.Export;
using BedrockBoot.Models.Pack.Game.Archive;
using BedrockBoot.Views.DialogContent.Export;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsControls : UserControl
{
    private readonly ArchiveInfo _info;

    public LevelSettingsControls()
    {
        InitializeComponent();
    }

    public LevelSettingsControls(ArchiveInfo info) : this()
    {
        _info = info;
    }

    private void ExportBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogExportWorldPackContent(_info);
        DialogHost.Show(new()
        {
            Title = "导出存档",
            Content = dialog,
            CloseButtonText = "导出",
            PrimaryButtonText = "取消",
            CloseAction = async () =>
            {
                var conf = new ExportConfig()
                {
                    ExportType = dialog.ArchiveExportType,
                    ArchiveInfo = _info,
                    AllowRandomSeed = dialog.ArchiveAllowRandomSeed,
                    LockTemplateOptions = dialog.ArchiveLockTemplateOptions,
                    PackVersion = dialog.ArchiveVersion,
                    PackName = string.IsNullOrEmpty(dialog.ArchiveName) ? _info.Name : dialog.ArchiveName,
                    PackDescription = dialog.ArchiveDescription
                };
                
                var extension = dialog.ArchiveExportType == ArchiveExportType.World ? "mcworld" : "mctemplate";
                
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null) return;

                var options = new FilePickerSaveOptions
                {
                    Title = "导出存档文件",
                    SuggestedFileName = $"{_info.Name}.{extension}",
                    DefaultExtension = extension,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Minecraft 支持文件") { Patterns = new[] { $"*.{extension}" } }
                    }
                };

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                if (file is not null)
                {
                    var filePath = file.Path.LocalPath;
                    var exp = new ArchiveExport(conf);

                    Task.Run(() =>
                    {
                        DialogHost.Show(new()
                        {
                            Title = "导出中...",
                            Content = "正在导出存档包"
                        });

                        exp.Export(filePath);

                        DialogHost.Close();

                        DialogHost.Show(new()
                        {
                            Title = "导出完毕",
                            Content = $"存档已导出至 {filePath}",
                            CloseButtonText = "确定"
                        });
                    });
                }
            }
        });
    }
}