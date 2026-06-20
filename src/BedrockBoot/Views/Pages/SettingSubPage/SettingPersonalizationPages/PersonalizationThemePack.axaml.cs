using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private bool _isUpdating;

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
            _isUpdating = false;

            PacksList.Items.Clear();

            InfoCard.IsVisible = false;
            LoadingCard.IsVisible = true;

            Task.Run(() =>
            {
                try
                {
                    var packs = Manager.GetPackManifests();
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            foreach (var conf in packs)
                            {
                                PacksList.Items.Add(new ThemePackItem(conf));
                            }

                            var selIndex = packs.FindIndex(x => x.IsSelectThis);
                            if (selIndex >= 0 && selIndex < PacksList.Items.Count)
                            {
                                PacksList.SelectedIndex = selIndex;
                            }

                            InfoCard.IsVisible = packs.Count <= 0;
                            LoadingCard.IsVisible = false;
                            _isUpdating = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($@"UI更新失败: {ex}");
                            InfoCard.IsVisible = true;
                            LoadingCard.IsVisible = false;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"读取主题包失败: {ex}");
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        InfoCard.IsVisible = true;
                        LoadingCard.IsVisible = false;
                    });
                }
            });
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
            if (!_isUpdating) return;
            
            try
            {
                var selectedIndex = PacksList.SelectedIndex;
                if (selectedIndex < 0) return;

                var packs = Manager.GetPackManifests();
                if (selectedIndex >= packs.Count) return;

                var selectedPack = packs[selectedIndex];
                if (selectedPack?.PackHash == null) return;

                GlobalModel.Config.Data.StyleConfig.SelectThemePackHash = selectedPack.PackHash;
                GlobalModel.Config.Save();
                
                Models.Global.GlobalModel.MainWindow.UpdateTheme();
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"切换主题包失败: {ex}");
            }
        }
    }
}