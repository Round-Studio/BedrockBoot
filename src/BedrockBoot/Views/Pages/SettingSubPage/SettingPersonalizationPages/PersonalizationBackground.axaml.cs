using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Enum;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages;

public partial class PersonalizationBackground : ISetting
{
    public PersonalizationBackground()
    {
        InitializeComponent();
        BackgroundTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.StyleType;
        OptBar.Value = GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity;
        BlurBar.Value = GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur;
        UpdateUI();

        if (OperatingSystem.IsWindows())
        {
            var osVersion = Environment.OSVersion;
            int buildNumber = osVersion.Version.Build;

            // Windows 版本判断逻辑
            if (osVersion.Version.Major == 10)
            {
                if (buildNumber >= 22000) // Win11
                {
                    MicaModel.IsEnabled = true;
                    BlurModel.IsEnabled = true;
                }
                else if (buildNumber >= 10240) // Win10
                {
                    BlurModel.IsEnabled = true;
                }
            }
        }
        
        MainSettingPage.SettingBreadcrumbBar.SetItems(new List<BreadcrumbItemInfo>()
        {
            new()
            {
                ItemName = "个性化",
                ItemClickAction = (info) =>
                    MainSettingPage.NavigationFrame.NavigateTo(new SettingPersonalization())
            },
            new()
            {
                ItemName = "背景"
            }
        });

        IsEdit = true;
    }

    private void UpdateUI()
    {
        IsEdit = false;
        BackgroundImageBox.IsVisible = false;
        
        if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image)
        {
            BackgroundImageBox.IsVisible = true;
            BackgroundsList.SelectedIndex = -1;
            BackgroundsList.Items.Clear();
            
            GlobalModel.Config.Data.StyleConfig.BackgroundImages.ForEach(image =>
            {
                var item = new BackgroundChooseItem() { ImagePath = image };
                item.UpdateUI();
                BackgroundsList.Items.Add(item);
            });

            var index = GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex;
            if (index != -1)
            {
                if (GlobalModel.Config.Data.StyleConfig.BackgroundImages.Count >= 0)
                {
                    BackgroundsList.SelectedIndex = index;
                }
            }
        }
        
        IsEdit = true;
    }
    private void BackgroundTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.StyleType = (StyleType)BackgroundTypeBox.SelectedIndex;
            GlobalModel.Config.Save();
            
            GlobalModel.MainWindow.UpdateBack();

            UpdateUI();
        }
    }

    private void BackgroundsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex = BackgroundsList.SelectedIndex;
            GlobalModel.Config.Save();
            
            GlobalModel.MainWindow.UpdateBack();
            App.LoadColor();
            UpdateUI();
        }
    }

    private async void ImportBackgroundBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
    
        // 2. 配置文件选择器选项
        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Choose Image File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.ImageAll
            }
        };
    
        // 3. 打开对话框并获取文件
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);
    
        // 4. 处理选中的文件
        if (files != null && files.Count > 0)
        {
            foreach (var file in files)
            {
                // 获取文件路径
                string filePath = file.Path.LocalPath;
                
                GlobalModel.Config.Data.StyleConfig.BackgroundImages.Add(filePath);
                UpdateUI();
            }
        }
        else
        {
            // 用户取消了选择
            Console.WriteLine(@"未选择文件。");
        }
    }

    private void OptBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (IsEdit)
        {
            GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity = (int)OptBar.Value;
            GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur = (int)BlurBar.Value;
        
            GlobalModel.Config.Save();
            GlobalModel.MainWindow.SetBackgroundBlur(GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur);
        }
    }
}