using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Instance;
using BedrockBoot.Services;
using BedrockBoot.Views.Pages.InstanceSubPage.UpdateContent;
using BedrockBoot.Views.TaskItem;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawUpdateInstanceContent : UserControl
{
    private static I18nManager i18n => I18nManager.Instance;
    private readonly VersionConfig _versionConfig;
    private InstanceUpdater? _updater;
    private BuildInfo? _selectedBuildInfo;
    private List<GameDownloadUrlInfo>? _sources;
    private bool _isSecondStep;

    public DrawUpdateInstanceContent()
    {
        InitializeComponent();
    }

    public DrawUpdateInstanceContent(VersionConfig value) : this()
    {
        _versionConfig = value;
        UpdateUi();
    }

    public async void UpdateUi()
    {
        _updater = new InstanceUpdater(_versionConfig)
        {
            ChooseDownloadUrl = (lst) => lst[0].Url
        };
        var versions = _updater.GetUpdateableVersions();

        if (versions.Count == 0)
        {
            LoadRing.IsVisible = false;
            NextBtn.IsEnabled = false;
            PreviousBtn.IsEnabled = false;
            NavigationFrame.IsVisible = false;
            InfoCard.IsVisible = true;
            return;
        }

        NavigationFrame.NavigateTo(new UpdateChooseVersion(versions));
        LoadRing.IsVisible = false;
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        PreviousBtn.IsVisible = _isSecondStep;
    }

    private void PreviousBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_isSecondStep) return;

        _isSecondStep = false;
        _sources = null;
        _selectedBuildInfo = null;

        if (_updater != null)
        {
            NavigationFrame.NavigateTo(new UpdateChooseVersion(_updater.GetUpdateableVersions()));
        }

        UpdateButtonState();
    }

    private async void NextBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_isSecondStep)
        {
            var currentPage = NavigationFrame.GetCurrentPage() as UpdateChooseVersion;
            var selectedVersion = currentPage?.SelectedBuildInfo;
            if (selectedVersion == null)
            {
                await ShowErrorAsync("请选择一个版本");
                return;
            }

            _selectedBuildInfo = selectedVersion;

            LoadRing.IsVisible = true;
            NextBtn.IsEnabled = false;

            try
            {
                _sources = await Task.Run(() => EasyDownload.GetPackageUrls(_selectedBuildInfo));
            }
            catch (Exception ex)
            {
                LoadRing.IsVisible = false;
                NextBtn.IsEnabled = true;
                await ShowErrorAsync($"{i18n["MainWindow.Dialog.Error.Title"]}: {ex.Message}");
                return;
            }

            LoadRing.IsVisible = false;
            NextBtn.IsEnabled = true;

            if (_sources == null || _sources.Count == 0)
            {
                await ShowErrorAsync("获取下载地址失败，请重试");
                return;
            }

            _isSecondStep = true;
            NavigationFrame.NavigateTo(new UpdateChooseSource(_selectedBuildInfo, _sources));
            UpdateButtonState();
        }
        else
        {
            var currentPage = NavigationFrame.GetCurrentPage() as UpdateChooseSource;
            var selectedUrl = currentPage?.SelectedUrl;
            if (string.IsNullOrEmpty(selectedUrl))
            {
                if (_sources != null && _sources.Count > 0)
                    selectedUrl = _sources[0].Url;
                else
                    return;
            }

            GlobalModel.MainWindow.CloseDraw();

            var taskItem = new TaskUpdateInstanceItem(_versionConfig, _selectedBuildInfo!, selectedUrl);
            var tuid = GlobalModel.TaskManager.AddTask(taskItem);
            taskItem.Start(() => GlobalModel.TaskManager.RemoveTask(tuid));
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DialogHost.Show(new DialogInfo
            {
                Title = i18n["MainWindow.Dialog.Error.Title"],
                Content = message,
                CloseButtonText = i18n["MainWindow.Common.Confirm"]
            });
        });
    }
}