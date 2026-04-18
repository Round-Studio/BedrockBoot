using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper.ExceptionHelper;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingUniversalPages;

public partial class UniversalException : ISettingPage
{
    public UniversalException()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Breadcrumb.Root"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Debug.Title"],
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new UniversalDebug())
            },
            new()
            {
                ItemName = I18nManager.Instance["Setting.Universal.Exception.Title"]
            }
        };

        UpdateUI();
    }

    public void UpdateUI()
    {
        Task.Run(() =>
        {
            var lst = ExceptionHelper.GetAllReport();
            if (lst.Count > 0)
                Dispatcher.UIThread.Invoke(() => InfoCard.IsVisible = false);

            lst.Reverse();

            lst.ForEach(re =>
            {
                Dispatcher.UIThread.Invoke(async () =>
                {
                    // 格式化崩溃描述：[时间] 发生的崩溃
                    var formattedTime = DateTime.Parse(re.ErrorTime).ToString("yyyy-MM-dd HH:mm:ss");
                    var description = string.Format(I18nManager.Instance["Setting.Universal.Exception.Item.Desc"],
                        formattedTime);

                    var item = new SettingCard
                    {
                        Header = re.ErrorTitle,
                        Description = description,
                        Glyph = "\uE730",
                        IsClickable = true
                    };

                    item.Click += async (s, e) =>
                    {
                        var topLevel = TopLevel.GetTopLevel(this);
                        if (topLevel == null) return;

                        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                        {
                            Title = I18nManager.Instance["Setting.Universal.Exception.Dialog.Save.Title"],
                            SuggestedFileName =
                                $"{I18nManager.Instance["Setting.Universal.Exception.Dialog.Save.Prefix"]} {Path.GetFileName(re.FileName)}",
                            DefaultExtension = "json",
                            FileTypeChoices = new[]
                            {
                                new FilePickerFileType(
                                    I18nManager.Instance["Setting.Universal.Exception.Dialog.Save.FileType"])
                                {
                                    Patterns = new[] { "*.json" }
                                }
                            },
                            ShowOverwritePrompt = true
                        });

                        if (file != null)
                        {
                            var filePath = file.Path.LocalPath;
                            await File.WriteAllTextAsync(filePath, re.ToJson());
                        }
                    };

                    ReportsList.Children.Add(item);
                });
            });
        });
    }
}