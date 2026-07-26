using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.DialogContent;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

/// <summary>
/// 设置 > 通用 > 软件更新 > 依赖更新。
/// 展示联机组件（EasyTier / GravityCone）已安装版本，检查并安装更新。
/// 仅 Windows 提供，Linux 下入口被隐藏。
/// </summary>
public partial class UniversalDependencyUpdate : ISettingPage
{
    public UniversalDependencyUpdate()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"],
                ItemClickAction = info => MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.SoftwareUpdate.Title"],
                ItemClickAction = info => MainSettingPage.NavigateTo(new UniversalSoftwareUpdate())
            },
            new()
            {
                ItemName = "依赖更新"
            }
        };

        RefreshLocalVersions();
    }

    /// <summary>展示本地已安装版本（不访问网络）</summary>
    private void RefreshLocalVersions()
    {
        EasyTierCard.Description = FormatLocal(
            MultiplayerDependencyHelper.IsEasyTierInstalled(),
            MultiplayerDependencyHelper.GetLocalVersion(MultiplayerDependencyHelper.EasyTierVersionFile));

        GravityConeCard.Description = FormatLocal(
            MultiplayerDependencyHelper.IsGravityConeInstalled(),
            MultiplayerDependencyHelper.GetLocalVersion(MultiplayerDependencyHelper.GravityConeVersionFile));

        EasyTierLatestText.Text = string.Empty;
        GravityConeLatestText.Text = string.Empty;
    }

    private static string FormatLocal(bool installed, string? version)
    {
        if (!installed) return "未安装";
        return string.IsNullOrEmpty(version)
            ? "已安装（无版本记录，建议更新一次以补齐记录）"
            : $"已安装版本：{version}";
    }

    private async void CheckUpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckUpdateBtn.IsEnabled = false;
        var oldContent = CheckUpdateBtn.Content;
        CheckUpdateBtn.Content = new ProgressRing
        {
            Width = 24,
            Height = 24,
            Background = Brushes.Transparent
        };

        try
        {
            var (easyTier, gravityCone) = await Task.Run(MultiplayerDependencyHelper.CheckUpdatesAsync);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EasyTierLatestText.Text = $"最新：{easyTier.LatestVersion}";
                GravityConeLatestText.Text = $"最新：{gravityCone.LatestVersion}";

                var anyNotInstalled = !easyTier.IsInstalled || !gravityCone.IsInstalled;
                var anyUpdate = easyTier.HasUpdate || gravityCone.HasUpdate;

                if (anyNotInstalled)
                {
                    UpdateCard.IsVisible = true;
                    UpdateCard.Header = "安装依赖";
                    UpdateCard.Description = "存在未安装的联机组件，点击下载安装最新版本";
                }
                else if (anyUpdate)
                {
                    UpdateCard.IsVisible = true;
                    UpdateCard.Header = "更新依赖";
                    UpdateCard.Description = "检测到新版本，点击下载并安装（更新期间联机功能将被关闭）";
                }
                else
                {
                    UpdateCard.IsVisible = false;
                    Models.Global.GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
                    {
                        Title = "依赖更新",
                        Message = "所有联机组件均为最新版本",
                        NoticeType = NoticeType.Success
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"检查依赖更新失败: {ex}");
            DialogHost.Show(new DialogInfo
            {
                Title = "检查更新失败",
                Content = $"无法从 GitHub 获取版本信息，请检查网络后重试。\n{ex.Message}",
                CloseButtonText = "确定"
            });
        }
        finally
        {
            CheckUpdateBtn.IsEnabled = true;
            CheckUpdateBtn.Content = oldContent;
        }
    }

    private void UpdateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // navigateAfterComplete: false — 从设置页更新完成后不去重新初始化联机页
        var dialog = new DialogDownloadMultiPlayerDependenceContent(false)
        {
            Completed = () =>
            {
                RefreshLocalVersions();
                UpdateCard.IsVisible = false;
                Models.Global.GlobalModel.MainWindow?.Notice?.AddNotice(new NoticeInfo
                {
                    Title = "依赖更新",
                    Message = "联机组件已更新完成",
                    NoticeType = NoticeType.Success
                });
            }
        };

        DialogHost.Show(new DialogInfo
        {
            Title = "更新联机依赖",
            Content = dialog
        });
    }
}
