using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Progress;
using BedrockBoot.Core.Models.Download;
using BedrockBoot.Models.Global;
using BedrockBoot.Proton;
using BedrockBoot.Proton.Entry.Info;
using BedrockBoot.Proton.Enum;
using BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.DialogContent.Linux;

public partial class DialogDownloadProtonGDKContent : UserControl
{
    public static string GameFixUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GameRunningFixKit.tar.gz";
    public static string ProtonXUserUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GDK-Proton-xuser.tar.gz";
    public static string ProtonLauncher =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/Proton-Launch-umu.tar.gz";
    public static string GamePatchUrl =>
        "https://github.com/RoundMCDev/ProtonGDK-Release/releases/download/Release10-32/GamePatch.zip";

    private StackPanel _mainPanel;
    private ProgressBar _mainProgressBar;
    private TextBlock _mainProgressText;
    private Dictionary<string, ProgressBar> _progressBars = new Dictionary<string, ProgressBar>();
    private Dictionary<string, TextBlock> _progressTexts = new Dictionary<string, TextBlock>();
    private Dictionary<string, string> _filePaths = new Dictionary<string, string>();
    private int _totalTasks;
    private int _completedTasks;

    public DialogDownloadProtonGDKContent()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        _mainPanel = new StackPanel();

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var progressRing = new ProgressRing
        {
            Height = 36,
            Width = 36,
            Background = Avalonia.Media.Brushes.Transparent
        };

        var textStack = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _mainProgressText = new TextBlock
        {
            FontSize = 16,
            Text = "下载 ProtonGDK"
        };

        _mainProgressBar = new ProgressBar
        {
            IsIndeterminate = true,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        textStack.Children.Add(_mainProgressText);
        textStack.Children.Add(_mainProgressBar);
        headerPanel.Children.Add(progressRing);
        headerPanel.Children.Add(textStack);
        _mainPanel.Children.Add(headerPanel);

        var tasksPanel = new StackPanel
        {
            Margin = new Thickness(0, 8, 0, 0)
        };

        var taskNames = new[] { "ProtonGDK", "GameRunningFixKit", "GDK-Proton-xuser", "Proton-Launch-umu", "GamePatch" };
        foreach (var name in taskNames)
        {
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4)
            };

            var nameText = new TextBlock
            {
                Text = name,
                Width = 150
            };

            var progressBar = new ProgressBar
            {
                MinWidth = 150,
                Margin = new Thickness(8, 0),
                IsIndeterminate = true
            };

            var progressText = new TextBlock
            {
                Text = "等待中",
                Margin = new Thickness(8, 0)
            };

            rowPanel.Children.Add(nameText);
            rowPanel.Children.Add(progressBar);
            rowPanel.Children.Add(progressText);
            tasksPanel.Children.Add(rowPanel);

            _progressBars[name] = progressBar;
            _progressTexts[name] = progressText;
        }

        _mainPanel.Children.Add(tasksPanel);
        Content = _mainPanel;
    }

    private void UpdateMainProgress()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_totalTasks > 0)
            {
                var totalProgress = (_completedTasks * 100.0) / _totalTasks;
                _mainProgressBar.IsIndeterminate = false;
                _mainProgressBar.Value = (int)totalProgress;
                _mainProgressText.Text = $"总进度 ({totalProgress:F2}%)";
            }
        });
    }

    public async Task Download()
    {
        ProtonCore.InitializeEnvironment();

        /*var lst = await ProtonCore.GetInstallableVersion(ProtonSource.LukasPAH);
        var info = lst?.ToList().FirstOrDefault();*/

        Task.Run(async () =>
        {
            try
            {
                var githubDownloader = new GithubFilesDownloader();
                string targetFolder = Path.Combine(PathsList.NeoProtonPath, "ProtonGDK_Components");
                Directory.CreateDirectory(targetFolder);

                var filesToDownload = new List<(string Name, string Url, string SavePath)>
                {
                    ("GameRunningFixKit", GameFixUrl, Path.Combine(targetFolder, "GameRunningFixKit.tar.gz")),
                    ("GDK-Proton-xuser", ProtonXUserUrl, Path.Combine(targetFolder, "GDK-Proton-xuser.tar.gz")),
                    ("Proton-Launch-umu", ProtonLauncher, Path.Combine(targetFolder, "Proton-Launch-umu.tar.gz")),
                    ("GamePatch", GamePatchUrl, Path.Combine(targetFolder, "GamePatch.zip"))
                };

                foreach (var file in filesToDownload)
                {
                    _filePaths[file.Name] = file.SavePath;
                }

                var tasks = new List<Task>();

                foreach (var file in filesToDownload)
                {
                    if (File.Exists(file.SavePath))
                    {
                        Dispatcher.UIThread.Invoke(() => { UpdateTaskProgress(file.Name, 100, "已存在"); });
                        _completedTasks++;
                        UpdateMainProgress();

                        await InstallComponent(file.Name);
                        continue;
                    }

                    var downloadTask = DownloadFileWithProgress(githubDownloader, file);
                    tasks.Add(downloadTask);
                }

                _totalTasks = tasks.Count;

                await Task.WhenAll(tasks);

                await Dispatcher.UIThread.InvokeAsync(DialogHost.Close);

                Dispatcher.UIThread.Invoke(() => { GameProton.UpdateList?.Invoke(); });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DialogHost.Show(new DialogInfo()
                    {
                        Title = "下载失败",
                        Content = $"下载过程中出现错误：{ex.Message}",
                        CloseButtonText = "确定",
                        CloseAction = () => { Environment.Exit(0); }
                    });
                });
            }
        });
    }

    private async Task InstallComponent(string componentName)
    {
        await Task.Run(() =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateTaskProgress(componentName, 50, "安装中...");
            });

            var filePath = _filePaths[componentName];

            switch (componentName)
            {
                case "GameRunningFixKit":
                    ZipHelper.ExtractTarGz(filePath, PathsList.NeoProtonPath, true);
                    break;
                case "GDK-Proton-xuser":
                    ZipHelper.ExtractTarGz(filePath, Path.Combine(PathsList.NeoProtonPath, "proton"), true);
                    break;
                case "Proton-Launch-umu":
                    ZipHelper.ExtractTarGz(filePath, PathsList.NeoProtonPath, true);
                    var umuRun = global::System.IO.Path.Combine(PathsList.NeoProtonPath, "umu", "umu-run");
                    if (global::System.IO.File.Exists(umuRun))
                        global::System.IO.File.SetUnixFileMode(umuRun,
                            global::System.IO.UnixFileMode.UserRead | global::System.IO.UnixFileMode.UserWrite | global::System.IO.UnixFileMode.UserExecute |
                            global::System.IO.UnixFileMode.GroupRead | global::System.IO.UnixFileMode.GroupExecute |
                            global::System.IO.UnixFileMode.OtherRead | global::System.IO.UnixFileMode.OtherExecute);
                    break;
                case "GamePatch":
                    ZipHelper.ExtractZipFile(filePath, PathsList.GamePatch, true);
                    break;
            }

            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateTaskProgress(componentName, 100, "安装完成");
            });
        });
    }

    private void UpdateTaskProgress(string taskName, int value, string text)
    {
        if (_progressBars.ContainsKey(taskName))
        {
            _progressBars[taskName].IsIndeterminate = false;
            _progressBars[taskName].Value = value;
            _progressTexts[taskName].Text = text;
        }
    }

    private async Task DownloadFileWithProgress(GithubFilesDownloader downloader, (string Name, string Url, string SavePath) file)
    {
        var progress = new Progress<DownloadProgress>(p =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateTaskProgress(file.Name, (int)p.ProgressPercentage, $"{p.ProgressPercentage:F2}%");
            });
        });

        bool success = await downloader.DownloadAsync(file.Url, file.SavePath, progress);
        if (!success)
        {
            throw new Exception($"组件 {file.Name} 下载失败");
        }

        _completedTasks++;
        UpdateMainProgress();

        await InstallComponent(file.Name);
    }
}