/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Standard.Entity.Game.Pack.ResourcePack.CurseForge;
using BedrockBoot.Standard.Entity.Info;
using BedrockBoot.Standard.Enum;
using BedrockBoot.Standard.Enum.Search;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Models.Translate;
using BedrockBoot.Service;
using BedrockBoot.Service.Download;
using BedrockBoot.Standard.Interface.Download;
using BedrockBoot.Views.Helper;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultRoot : UserControl
{
    private ImageLoader imageLoader = new ImageLoader();

    public ResultRoot()
    {
        InitializeComponent();
    }

    public ResultRoot(SearchResultItemInfo info) : this()
    {
        SearchResultItemInfo = info;
        _ = UpdateAsync();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        imageLoader.Dispose();

#if WINDOWS
        // 详情页关闭后整理一次工作集，把内存还给系统
        BedrockBoot.Windows.Models.ProcessMemoryTrimmer.TrimProcess();
#endif
    }

    private static I18nManager i18n => I18nManager.Instance;

    public SearchResultItemInfo SearchResultItemInfo { get; set; } = null!;
    private IDownloadResult _downloadService;
    private ResultDescription? _descriptionPage;

    private int _githubErrorShown;
    private bool _isDescriptionTranslated;
    private bool _isTranslating;
    private string? _translatedBrief;

    public async Task UpdateAsync()
    {
        _downloadService = SearchResultItemInfo.ResourceType switch
        {
            SearchResourceType.ResourcePack => new CurseForgeDownloadResult(SearchResultItemInfo),
            SearchResourceType.PluginPack => new PluginDownloadResult(SearchResultItemInfo),
            SearchResourceType.LeviLaminaMods => new LeviLaminaDownloadResult(SearchResultItemInfo),
            SearchResourceType.DllMods => new DllModDownloadResult(SearchResultItemInfo)
        };

        ResourceName.Text = SearchResultItemInfo.Name;
        AuthorText.Text = $"{i18n["Download.Result.Author.Prefix"]} {string.Join(", ", SearchResultItemInfo.Authors)}";
        ResourceName2.Text = SearchResultItemInfo.Name;
        AuthorText2.Text = $"{string.Join(", ", SearchResultItemInfo.Authors)}";
        DescriptionText.Text = SearchResultItemInfo.Description;
        UpdataDateText.Text = DateHelper.GetRelativeTime(SearchResultItemInfo.DateUpdated);
        InstalledBox.IsVisible = false;
        ItemFiles.IsVisible = _downloadService.IsHasManyFiles;
        UpdateTimePanel.IsVisible =
            SearchResultItemInfo.DateUpdated != default &&
            SearchResultItemInfo.DateUpdated.Year > 1;

        var hasWebsite = !string.IsNullOrEmpty(SearchResultItemInfo.SourceWebsite);
        HyperlinkButton.IsVisible = hasWebsite;
        HyperlinkButton2.IsVisible = hasWebsite;
        if (hasWebsite && Uri.TryCreate(SearchResultItemInfo.SourceWebsite, UriKind.Absolute, out var uri))
        {
            HyperlinkButton2.NavigateUri = uri;
            HyperlinkButton.NavigateUri = uri;
        }

        LabelsBox.Children.Clear();
        if (SearchResultItemInfo.Labels is { Count: > 0 })
        {
            LabelsBox.IsVisible = true;
            foreach (var s in SearchResultItemInfo.Labels) LabelsBox.Children.Add(new LabelBox { Text = s });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var count = await _downloadService.GetDownloadCount();
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    DownloadCountText.Text = count.ToString("N0");
                    DownloadCountPanel.IsVisible = true;
                });
            }
            catch (Exception ex)
            {
                await ShowGitHubErrorOnce(ex);
            }
        });

        _ = Task.Run(async () =>
        {
            try
            {
                var installed = await _downloadService.IsInstalled();
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    InstalledBox.IsVisible = installed;
                    GetResourceBtn.IsVisible = !installed;
                });
            }
            catch (Exception ex)
            {
                await ShowGitHubErrorOnce(ex);
            }
        });

        // 详情页主图标显示 90x90，按 180 宽解码（兼容 2x）
        var icon = await imageLoader.LoadImageBrushAsync(SearchResultItemInfo.IconUri, true, 180);

        if (icon != null)
        {
            IconBox.Source = icon;
            ResourceIcon.Source = icon;
            ResourceIconIcon.IsVisible = false;
            IconFont.IsVisible = false;
        }
    }

    private async Task ShowGitHubErrorOnce(Exception exception)
    {
        if ((SearchResultItemInfo.ResourceType is SearchResourceType.PluginPack or
             SearchResourceType.LeviLaminaMods or SearchResourceType.DllMods) &&
            Interlocked.Exchange(ref _githubErrorShown, 1) == 0)
            await GitHubErrorDialog.ShowAsync(exception);
    }

    private void GetResourceBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_downloadService.IsHasManyFiles)
            PageSelect.SelectedItem = ItemFiles;
        else
            _downloadService.Install();
    }

    private void CopyName_OnClick(object? sender, RoutedEventArgs e)
    {
        var shareContent = string.Format(i18n["Download.Result.Share.Format"],
            SearchResultItemInfo.Name, SearchResultItemInfo.SourceWebsite);

        CopyService.SetClipboard(shareContent, SearchResultItemInfo);

        GlobalModel.MainWindow.Notice.AddNotice(new NoticeInfo
        {
            Title = "剪切板",
            Message = i18n["Download.Result.Share.Success"]
        });
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (MainScrollViewer != null)
        {
            var value = MainScrollViewer.Offset.Y;
            if (value >= 120)
                SmallBox.Margin = new Thickness(30, 25, 30, 0);
            else
                SmallBox.Margin = new Thickness(30, -72, 30, 0);
        }
    }

    private void GoTopBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        MainScrollViewer.Offset = new Vector(MainScrollViewer.Offset.X, 0);
    }

    private void PageSelect_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PageSelect == null) return;
        if (PageSelect.SelectedItem == null) return;
        var tag = (PageSelect.SelectedItem as ListBoxItem)!.Tag!.ToString();
        UserControl page = tag switch
        {
            "Description" => new ResultDescription(_downloadService)
            {
                NotFountDescription = () =>
                {
                    if (_downloadService.IsHasManyFiles)
                    {
                        ItemDescription.IsVisible = false;
                        PageSelect.SelectedItem = ItemFiles;
                    }
                    else
                    {
                        PagesSelBar.IsVisible = false;
                        NavigationFrame.IsVisible = false;
                    }
                }
            },
            "Files" => new ResultFiles(_downloadService)
        };

        _descriptionPage = page as ResultDescription;
        NavigationFrame.NavigateTo(page);
    }

    private async void TranslateBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isTranslating || _downloadService == null || SearchResultItemInfo == null) return;

        _isTranslating = true;
        TranslateBtn.IsEnabled = false;
        try
        {
            if (!_isDescriptionTranslated)
            {
                TranslateBtnText.Text = i18n["Download.Result.Translate.Processing"];

                var target = BedrockBoot.Core.Global.GlobalModel.Config.Data.Language;
                var translator = new MicrosoftTranslateService();

                // 批量翻译 + 本地缓存：先查缓存，未命中的才走接口，结果统一回填缓存
                Func<IReadOnlyList<string>, Task<IReadOnlyList<string>?>> translateBatch = async texts =>
                {
                    var cached = TranslationCache.GetOrVisit(texts, target);
                    var missTexts = new List<string>();
                    var missIndexes = new List<int>();
                    for (var i = 0; i < texts.Count; i++)
                        if (cached[i] == null)
                        {
                            missIndexes.Add(i);
                            missTexts.Add(texts[i]);
                        }

                    var results = new string[texts.Count];
                    for (var i = 0; i < texts.Count; i++)
                        results[i] = cached[i] ?? texts[i];

                    if (missTexts.Count > 0)
                    {
                        var fetched = await translator.TranslateTextBatchAsync(missTexts, "auto", target);
                        if (fetched == null) return null;

                        var fresh = new Dictionary<string, string>();
                        for (var i = 0; i < missTexts.Count; i++)
                        {
                            // 接口对不可译内容原样返回，空结果不入缓存
                            if (string.IsNullOrWhiteSpace(fetched[i]) ||
                                string.Equals(fetched[i], missTexts[i], StringComparison.Ordinal))
                                continue;

                            results[missIndexes[i]] = fetched[i];
                            fresh[missTexts[i]] = fetched[i];
                        }

                        TranslationCache.Put(fresh);
                    }

                    return results;
                };

                // 页面顶部的简介短文本
                var brief = SearchResultItemInfo.Description;
                if (!string.IsNullOrWhiteSpace(brief))
                {
                    var briefResult = await translateBatch(new[] { brief });
                    if (briefResult != null && !string.Equals(briefResult[0], brief, StringComparison.Ordinal))
                        _translatedBrief = briefResult[0];
                }

                var done = false;
                if (_descriptionPage != null)
                    done = await _descriptionPage.TranslateContentAsync(translateBatch,
                        (doneCount, total) =>
                        {
                            // 进度只在按钮仍处于「翻译中」时刷新，
                            // 避免排队的进度回调把完成态的「显示原文」覆盖掉
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (_isTranslating)
                                    TranslateBtnText.Text =
                                        string.Format(i18n["Download.Result.Translate.Progress"], doneCount, total);
                            });
                        });

                if (_translatedBrief != null) DescriptionText.Text = _translatedBrief;
                if (done)
                {
                    _isDescriptionTranslated = true;
                    TranslateBtnText.Text = i18n["Download.Result.Translate.Restore"];
                }
                else
                {
                    TranslateBtnText.Text = i18n["Download.Result.Translate.Action"];
                }
            }
            else
            {
                _isDescriptionTranslated = false;
                DescriptionText.Text = SearchResultItemInfo.Description;
                _descriptionPage?.RestoreOriginal();
                TranslateBtnText.Text = i18n["Download.Result.Translate.Action"];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Failed to translate resource description: {ex.Message}");
            TranslateBtnText.Text = i18n[_isDescriptionTranslated
                ? "Download.Result.Translate.Restore"
                : "Download.Result.Translate.Action"];
        }
        finally
        {
            _isTranslating = false;
            TranslateBtn.IsEnabled = true;
        }
    }

    private void DeleteBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _downloadService.Delete();
    }

    private void ReInstallBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        _downloadService.ReInstall();
    }
}