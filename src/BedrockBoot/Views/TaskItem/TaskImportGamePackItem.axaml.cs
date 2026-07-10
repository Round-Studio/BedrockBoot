using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game.Pack.Import;
using BedrockBoot.Base.Entry.Task;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Import;
using BedrockLauncher.Core;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem;

public partial class TaskImportGamePackItem : UserControl, ITaskItem
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

    private CancellationTokenSource _cts;
    private double _taskProgress;
    private string _taskStatusText = "";
    private string _taskTitle = "";
    private bool _taskIsCompleted;
    private bool _taskIsIndeterminate = true;

    public double Progress => _taskProgress;
    public string StatusText => _taskStatusText;
    public string Title => _taskTitle;
    public bool IsCompleted => _taskIsCompleted;
    public bool IsIndeterminate => _taskIsIndeterminate;

    public event Action<ITaskItem>? ProgressUpdated;

    protected void ReportProgress(double progress, string statusText, bool isIndeterminate = false)
    {
        _taskProgress = progress;
        _taskStatusText = statusText;
        _taskIsIndeterminate = isIndeterminate;
        if (progress >= 100) _taskIsCompleted = true;
        ProgressUpdated?.Invoke(this);
    }

    public async Task Install(Action installed)
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
                ReportProgress(currentProgress, s.StatusMessage);

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

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            await body.Install(InstallFolder, InstallName, token);
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
    }

    public static async void Install(string packFile, string installFolder, string installName, MinecraftGameTypeVersion type,
        bool knowGameTypeCheckBox)
    {
        var body = new TaskImportGamePackItem(packFile, installFolder, installName, type, knowGameTypeCheckBox);
        var tuid = GlobalModel.TaskManager.AddTask(body);

        await body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }
}