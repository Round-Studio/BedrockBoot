using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportGamePackItem : UserControl
{
    public TaskImportGamePackItem()
    {
        InitializeComponent();
    }

    public TaskImportGamePackItem(string packFile, string installFolder, string installName,
        MinecraftGameTypeVersion type, bool knowGameType) : this()
    {
        PackFile = packFile;
        InstallName = installName;
        InstallFolder = installFolder;
        GDKGameType = type;
        IsGDKUnknownBuildType = knowGameType;
    }

    public string PackFile { get; set; }
    public string InstallFolder { get; set; }
    public string InstallName { get; set; }
    public bool IsGDKUnknownBuildType { get; set; }
    public MinecraftGameTypeVersion GDKGameType { get; set; } = MinecraftGameTypeVersion.Release;

    public void Install(Action installed)
    {
        var body = new PackInstaller(PackFile)
        {
            GDKGameType = GDKGameType,
            IsGDKUnknownBuildType = IsGDKUnknownBuildType
        };
        double lastProgress = -1;

        body.ImportProgress = new Progress<PackImportProgress>(s =>
        {
            var currentProgress = Math.Round(s.Progress, 2);

            if (Math.Abs(currentProgress - lastProgress) > 0.0001)
            {
                lastProgress = currentProgress;

                Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressBar.Value = (int)s.Progress;
                    // 注意：s.StatusMessage 如果来自核心库硬编码，建议后续在核心库也进行 I18n 处理
                    ProgressText.Text = s.StatusMessage;

                    if (ProgressBar.IsIndeterminate)
                        ProgressBar.IsIndeterminate = false;
                });
            }
        });
        body.ImportedAction = () => installed.Invoke();

        Task.Run(async () =>
        {
            try
            {
                await body.Install(InstallFolder, InstallName);
            }
            catch (Exception e)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    DialogHost.Show(new DialogInfo
                    {
                        // 错误信息国际化
                        Title = string.Format(I18nManager.Instance["Task.ImportPack.Error.Title"], InstallName),
                        Content = I18nManager.Instance["Task.ImportPack.Error.Content"],
                        CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"]
                    });
                });
            }
        });
    }

    public static void Install(string packFile, string installFolder, string installName, MinecraftGameTypeVersion type,
        bool knowGameTypeCheckBox)
    {
        var body = new TaskImportGamePackItem(packFile, installFolder, installName, type, knowGameTypeCheckBox);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }
}