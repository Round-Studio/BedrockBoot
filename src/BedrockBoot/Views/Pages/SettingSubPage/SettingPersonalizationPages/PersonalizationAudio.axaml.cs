using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Enum;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Models.Media;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages
{
    public partial class PersonalizationAudio : ISettingPage
    {
        private bool _isUpdating;

        public PersonalizationAudio()
        {
            InitializeComponent();

            VolumeSlider.Value = GlobalModel.Config.Data.MediaVolume * 100;

            GlobalModel.Config.AfterSave += OnConfigAfterSave;

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
                    ItemName = "音频"
                }
            };

            IsPlayBackgroundMusic.IsChecked = GlobalModel.Config.Data.IsPlayBackgroundMusic;
            MediaSource.SelectedIndex = (int)GlobalModel.Config.Data.StyleConfig.MediaSource;

            IsEdit = true;
        }

        /// <summary>
        /// AfterSave 挂在全局配置对象上，页面被导航替换后若不取消订阅会导致整个页面被永久持有
        /// </summary>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            GlobalModel.Config.AfterSave -= OnConfigAfterSave;
        }

        private void OnConfigAfterSave(object? sender, EventArgs e)
        {
            var configVolume = GlobalModel.Config.Data.MediaVolume * 100;
            if (Math.Abs(VolumeSlider.Value - configVolume) > 0.01)
            {
                _isUpdating = true;
                VolumeSlider.Value = configVolume;
                _isUpdating = false;
            }
        }

        private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isUpdating) return;

            var newVolume = VolumeSlider.Value / 100.0;
            GlobalModel.Config.Data.MediaVolume = newVolume;
            // 滑块拖动为高频事件，合并写盘
            ConfigSaveScheduler.RequestSave();
            MediaManager.Instance.Volume = (float)newVolume;
        }

        private async void ChooseAudioFileBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            // 配置文件选择器选项
            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "选择背景音频",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Audio Files")
                    {
                        Patterns = new[] { "*.mp3", "*.wav", "*.ogg", "*.flac" }
                    }
                }
            };

            // 打开对话框并获取文件
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);

            // 处理选中的文件
            if (files != null && files.Count > 0)
                foreach (var file in files)
                {
                    var filePath = file.Path.LocalPath;
                    GlobalModel.Config.Data.StyleConfig.BackgroundMusic = filePath;
                    GlobalModel.Config.Save();
                    Models.Global.GlobalModel.MainWindow.UpdateTheme();
                }
        }

        private void IsPlayBackgroundMusic_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.IsPlayBackgroundMusic = IsPlayBackgroundMusic.IsChecked ?? false;
                GlobalModel.Config.Save();
            
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }

        private void RestoreDefaultAudioBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            GlobalModel.Config.Data.StyleConfig.BackgroundMusic = string.Empty;
            GlobalModel.Config.Save();
            Models.Global.GlobalModel.MainWindow.UpdateTheme();
        }

        private void MediaSource_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.MediaSource = (MediaSourceEnum)MediaSource.SelectedIndex;
                GlobalModel.Config.Save();
            
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }
    }
}