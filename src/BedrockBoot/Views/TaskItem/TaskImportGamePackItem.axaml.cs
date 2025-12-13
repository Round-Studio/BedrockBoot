using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Import;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportGamePackItem : UserControl
{
    public string PackFile { get; set; }
    public string InstallFolder { get; set; }
    public string InstallName { get; set; }
    public TaskImportGamePackItem()
    {
        InitializeComponent();
    }

    public TaskImportGamePackItem(string packFile, string installFolder, string installName) : this()
    {
        PackFile = packFile;
        InstallName = installName;
        InstallFolder = installFolder;
    }

    public async void Install(Action installed)
    {
        var body = new PackInstaller(PackFile);
        double lastProgress = -1;
        body.ImportProgress = new Progress<PackImportProgress>((s) =>
        {
            // 精确到小数点后两位进行比较
            double currentProgress = Math.Round(s.Progress, 2);
    
            // 只有当小数点后两位的值变化时才刷新UI
            if (Math.Abs(currentProgress - lastProgress) > 0.0001) // 浮点数比较容差
            {
                lastProgress = currentProgress;
                Console.WriteLine($"{s.StatusMessage} - {currentProgress:F2} %");

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

        Task.Run(() => body.Install(InstallFolder, InstallName));
    }

    public static void Install(string packFile, string installFolder, string installName)
    {
        var body = new TaskImportGamePackItem(packFile, installFolder, installName);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }
}