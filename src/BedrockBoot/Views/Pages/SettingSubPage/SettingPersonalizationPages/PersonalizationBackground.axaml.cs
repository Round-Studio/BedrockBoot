using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using File = System.IO.File;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages
{
    public partial class PersonalizationBackground : ISettingPage
    {
        public bool IsEdit;

        public PersonalizationBackground()
        {
            InitializeComponent();
            BackgroundTypeBox.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.StyleType;
            OptBar.Value = GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity;
            BlurBar.Value = GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur;
            Image3D.IsChecked = GlobalModel.Config.Data.StyleConfig.Background3D;
            ImageQuality.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.ImageQuality;
            UpdateUI();
            
            MicaModel.IsEnabled = false;
            BlurModel.IsEnabled = false;
            LiveModel.IsEnabled = false;

            if (OperatingSystem.IsWindows())
            {
                var osVersion = Environment.OSVersion;
                var buildNumber = osVersion.Version.Build;

                LiveModel.IsEnabled = true;

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

            BreadcrumbItem = new List<BreadcrumbItemInfo>
            {
                new()
                {
                    ItemName = I18nManager.Instance["Setting.Personalization.Breadcrumb.Root"],
                    ItemClickAction = info =>
                        MainSettingPage.NavigateTo(new SettingPersonalization())
                },
                new()
                {
                    ItemName = I18nManager.Instance["Setting.Personalization.Background.Title"]
                }
            };

            IsEdit = true;
        }

        private void UpdateUI()
        {
            IsEdit = false;
            BackgroundImageBox.IsVisible = false;
            LiveOptCard.IsVisible = false;

            if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image)
            {
                BackgroundImageBox.IsVisible = true;
            }

            LiveOptCard.IsVisible = GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.LiveModel;
            LiveBlurCard.IsVisible = GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.LiveModel;
            LiveOptBar.Value = GlobalModel.Config.Data.StyleConfig.LiveOpacity;
            LiveBlurSwitch.IsChecked = GlobalModel.Config.Data.StyleConfig.LiveBlur;

            IsEdit = true;
        }

        private void BackgroundTypeBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.StyleType = (StyleType)BackgroundTypeBox.SelectedIndex;
                GlobalModel.Config.Save();

                Models.Global.GlobalModel.MainWindow.UpdateTheme();

                UpdateUI();
            }
        }

        private async void ImportBackgroundBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            // 配置文件选择器选项
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = I18nManager.Instance["Setting.Personalization.Background.Dialog.Import.Title"],
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    FilePickerFileTypes.ImageAll
                }
            };

            // 打开对话框并获取文件
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

            // 处理选中的文件
            if (files != null && files.Count > 0)
                foreach (var file in files)
                {
                    var filePath = file.Path.LocalPath;
                    GlobalModel.Config.Data.StyleConfig.BackgroundImage = filePath;
                    GlobalModel.Config.Save();
                    UpdateUI();
                    Models.Global.GlobalModel.MainWindow.UpdateTheme();
                }
        }

        private void OptBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity = (int)OptBar.Value;
                GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur = (int)BlurBar.Value;

                GlobalModel.Config.Save();
                Models.Global.GlobalModel.MainWindow.SetBackgroundBlur(GlobalModel.Config.Data.StyleConfig
                    .BackgroundImageBlur);
            }
        }

        private void Image3D_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.Background3D = Image3D.IsChecked ?? false;

                GlobalModel.Config.Save();
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }

        private void LiveOptBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.LiveOpacity = (int)LiveOptBar.Value;

                GlobalModel.Config.Save();
                Models.Global.GlobalModel.MainWindow.UpdateLiveOpacity();
            }
        }

        private void LiveBlurSwitch_OnClick(object? sender, RoutedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.LiveBlur = LiveBlurSwitch.IsChecked ?? false;
                GlobalModel.Config.Save();
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }

        private void RestoreDefaultBackgroundBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            GlobalModel.Config.Data.StyleConfig.BackgroundImage = string.Empty;
            GlobalModel.Config.Save();
            UpdateUI();
            Models.Global.GlobalModel.MainWindow.UpdateTheme();
        }

        private void ImageQuality_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.ImageQuality = (ImageQuality)ImageQuality.SelectedIndex;
                GlobalModel.Config.Save();
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }
    }
}