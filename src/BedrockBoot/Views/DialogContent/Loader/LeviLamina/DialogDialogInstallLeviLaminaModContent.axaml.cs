using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BedrockBoot.Models.Pack.LeviLamina;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogInstallLeviLaminaModContent : UserControl
{
    private readonly string _versionId;
    private readonly string _savePath;
    private readonly string _key;
    private readonly bool _isOnlyDownload;
    private LipClient _client;

    public DialogInstallLeviLaminaModContent()
    {
        InitializeComponent();
    }

    public DialogInstallLeviLaminaModContent(string versionId, string savePath, string key, bool isOnlyDownload = false)
        : this()
    {
        _versionId = versionId;
        _savePath = savePath;
        _key = key;
        _isOnlyDownload = isOnlyDownload;

        Install();
    }

    public async void Install()
    {
        try
        {
            var toothKey = $"{_key}#client@{_versionId}";
            _client = new LipClient(_savePath);

            _client.ProgressChanged += (sender, progress) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    InstallProgressBar.IsIndeterminate = false;
                    InstallProgressText.Text = $"{progress} %";
                    InstallProgressBar.Value = progress;

                    // 当进度达到100%时关闭对话框
                    if (progress >= 100)
                    {
                        DialogHost.Close();
                    }
                });
            };

            await _client.InstallAsync(toothKey);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                InstallProgressText.Text = $"安装失败: {ex.Message}";
                InstallProgressBar.IsIndeterminate = false;
            });

            // 延迟关闭对话框，让用户看到错误信息
            await Task.Delay(3000);
            Dispatcher.UIThread.Invoke(DialogHost.Close);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }
}