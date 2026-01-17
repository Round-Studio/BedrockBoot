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
    public string PackFile { get; set; }
    public string InstallFolder { get; set; }
    public string InstallName { get; set; }
    public bool IsGDKUnknownBuildType { get; set; } = false;
    public MinecraftGameTypeVersion GDKGameType { get; set; } = MinecraftGameTypeVersion.Release;
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

    public async void Install(Action installed)
    {
        var body = new PackInstaller(PackFile)
        {
            GDKGameType = GDKGameType,
            IsGDKUnknownBuildType = IsGDKUnknownBuildType
        };
        double lastProgress = -1;
        body.ImportProgress = new Progress<PackImportProgress>((s) =>
        {
            // 精确到小数点后两位进行比较
            double currentProgress = Math.Round(s.Progress, 2);
    
            // 只有当小数点后两位的值变化时才刷新UI
            if (Math.Abs(currentProgress - lastProgress) > 0.0001) // 浮点数比较容差
            {
                lastProgress = currentProgress;
                Console.WriteLine($@"{s.StatusMessage} - {currentProgress:F2} %");

                Dispatcher.UIThread.Invoke(() =>
                {
                    ProgressBar.Value = (int)s.Progress;
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
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = $"抱歉，无法安装 {InstallName}",
                        Content = "您的包可能存在问题，是不支持的格式",
                        CloseButtonText = "确定"
                    });
                });
            }
        });
    }

    public static void Install(string packFile, string installFolder, string installName, MinecraftGameTypeVersion type,bool knowGameTypeCheckBox)
    {
        var body = new TaskImportGamePackItem(packFile, installFolder, installName, type,knowGameTypeCheckBox);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }
}