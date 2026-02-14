using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
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
                ItemName = "通用",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new SettingUniversal())
            },
            new()
            {
                ItemName = "调试模式",
                ItemClickAction = info =>
                    MainSettingPage.NavigateTo(new UniversalDebug())
            },
            new()
            {
                ItemName = "崩溃记录"
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
                Dispatcher.UIThread.Invoke(() =>
                {
                    var item = new SettingCard()
                    {
                        Header = re.ErrorTitle,
                        Description = $"{DateTime.Parse(re.ErrorTime).ToString("yyyy-MM-dd HH:mm:ss")} 发生的崩溃",
                        Glyph = "\uE730",
                        IsClickable = true
                    };

                    item.Click += async (s, e) =>
                    {
                        var topLevel = TopLevel.GetTopLevel(this);

                        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                        {
                            Title = "保存错误报告",
                            SuggestedFileName = $"错误报告 {Path.GetFileName(re.FileName)}",
                            DefaultExtension = "json",
                            FileTypeChoices = new[]
                            {
                                // 定义可选择的文件类型过滤器
                                new FilePickerFileType("BedrockBoot 崩溃报告")
                                {
                                    Patterns = new[] { "*.json" }
                                }
                            },
                            ShowOverwritePrompt = true
                        });

                        if (file != null)
                        {
                            var filePath = file.Path.LocalPath;
                            File.WriteAllText(filePath, re.ToJson());
                        }
                    };

                    ReportsList.Children.Add(item);
                });
            });
        });
    }
}