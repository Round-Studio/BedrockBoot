using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Management.Deployment;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Global;
using BedrockBoot.Services;
using BedrockBoot.Views.TaskItem;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.TaskItem
{
    public partial class TaskDownloadGameItem : UserControl
    {
        private EasyDownload _downloader;

        public string InstallFolder { get; set; }
        public string GameName { get; set; }
        public string Url { get; set; }
        public BuildInfo BuildInfo { get; set; }

        public TaskDownloadGameItem()
        {
            InitializeComponent();
        }

        public TaskDownloadGameItem(BuildInfo info, string url, string dir, string gameName) : this()
        {
            BuildInfo = info;
            InstallFolder = dir;
            GameName = gameName;
            Url = url;

            InitializeDownloader();
        }

        private void InitializeDownloader()
        {
            _downloader = new EasyDownload(BuildInfo, InstallFolder, GameName)
            {
                DownloadProgress = (text, percentage) =>
                    Dispatcher.UIThread.Invoke(() => UpdateDownloadProgress(text, percentage)),

                DownloadSpeed = speed =>
                    Dispatcher.UIThread.Invoke(() => MainSpeedText.Text = speed),

                // 新增合并进度回调
                MergeProgress = (text, percentage) =>
                    Dispatcher.UIThread.Invoke(() => UpdateMergeProgress(text, percentage)),

                ExtractionProgress = (text, percentage) =>
                    Dispatcher.UIThread.Invoke(() => UpdateExtractionProgress(text, percentage)),

                DeploymentProgress = (text, progress) =>
                    Dispatcher.UIThread.Invoke(() => UpdateDeploymentProgress(text, progress)),

                StatusText = text =>
                    Dispatcher.UIThread.Invoke(() => MainText.Text = text),

                InstallStateChanged = states =>
                    Dispatcher.UIThread.Invoke(() => HandleInstallState(states)),

                ErrorOccurred = (title, message, ex) =>
                    Dispatcher.UIThread.Invoke(() => ShowErrorDialog(title, message, ex))
            };
        }

        private void UpdateMergeProgress(string text, double percentage)
        {
            InsMergeBar.IsIndeterminate = false;
            InsMergeBar.Value = percentage;
            MainText.Text = text;
        }

        public void Install(Action installed)
        {
            CardTitle.Text = $"下载游戏 {BuildInfo.ID}";

            InsGetUrlBar.IsIndeterminate = true;
            if (BuildInfo.BuildType == MinecraftBuildTypeVersion.GDK)
                InsInstallGamePanel.IsVisible = false;

            Task.Run(async () =>
            {
                await _downloader.InstallAsync(Url);
                installed?.Invoke();
            });
        }

        private void UpdateDownloadProgress(string text, double percentage)
        {
            if (InsGetUrlBar.IsIndeterminate)
            {
                InsGetUrlBar.IsIndeterminate = false;
                InsGetUrlBar.Value = 100;
            }

            InsDownGameBar.Value = percentage;
            MainText.Text = text;
        }

        private void UpdateExtractionProgress(string text, double percentage)
        {
            InsUnZipBar.IsIndeterminate = false;
            InsUnZipBar.Value = percentage;
            MainText.Text = text;
        }

        private void UpdateDeploymentProgress(string text, DeploymentProgress progress)
        {
            InsInstallGameBar.Value = progress.percentage;
            MainText.Text = text;
        }

        private void HandleInstallState(InstallStates states)
        {
            switch (states)
            {
                case InstallStates.Extracting:
                    InsUnZipBar.IsIndeterminate = false;
                    break;

                case InstallStates.Extracted:
                    InsUnZipBar.Value = 100;
                    MainSpeedText.Text = "本地安装中...";
                    break;

                case InstallStates.Cleared:
                    InsInstallGameBar.IsIndeterminate = true;
                    break;

                case InstallStates.Registering:
                    InsInstallGameBar.IsIndeterminate = false;
                    break;

                case InstallStates.Registered:
                    InsInstallGameBar.Value = 100;
                    break;
            }
        }

        private void ShowErrorDialog(string title, string message, Exception ex)
        {
            if (ex != null)
            {
                message += $"\n\n错误详情：{ex.Message}";
            }

            DialogHost.Show(new DialogInfo()
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                AccountButton = DialogButtons.CloseButton
            });
        }

        public static void Install(BuildInfo info, string url, string dir, string gameName)
        {
            var body = new TaskDownloadGameItem(info, url, dir, gameName);
            var tuid = GlobalModel.TaskManager.AddTask(body);

            body.Install(() => { GlobalModel.TaskManager.RemoveTask(tuid); });
        }
    }
}