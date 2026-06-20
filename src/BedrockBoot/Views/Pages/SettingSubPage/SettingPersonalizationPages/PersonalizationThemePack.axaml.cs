using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Core.Global;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Plugin;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages
{
    public partial class PersonalizationThemePack : ISettingPage
    {
        public ThemePackManager Manager { get; set; } = new ThemePackManager();

        public PersonalizationThemePack()
        {
            InitializeComponent();
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
                    ItemName = "主题包"
                }
            };

            UpdateUI();
        }

        public void UpdateUI()
        {
            IsEdit = false;

            PacksList.Items.Clear();

            InfoCard.IsVisible = false;
            LoadingCard.IsVisible = true;

            var packs = Manager.GetPackManifests();
            packs.ForEach(conf => { PacksList.Items.Add(new ThemePackItem(conf)); });

            var selIndex = packs.FindIndex(x => x.IsSelectThis);
            PacksList.SelectedIndex = selIndex;

            InfoCard.IsVisible = packs.Count <= 0;
            LoadingCard.IsVisible = false;

            IsEdit = true;
        }

        private async void ImportThemePack_OnClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "导入主题包",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("BedrockBoot 主题包文件")
                    {
                        Patterns = new[] { "*.rskin" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var selectedPath = files[0].Path.LocalPath;
                Manager.AddPack(selectedPath);
                UpdateUI();
            }
        }

        private void PacksList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                GlobalModel.Config.Data.StyleConfig.SelectThemePackHash = Manager.GetPackManifests()[PacksList.SelectedIndex].PackHash!;
                GlobalModel.Config.Save();
                
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
        }
    }
}