using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Theme;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingPersonalizationPages
{
    public partial class PersonalizationThemePack : ISettingPage
    {
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

            InfoCard.IsVisible = false;
            LoadingCard.IsVisible = true;

            var manager = new ThemePackManager();
        }
    }
}