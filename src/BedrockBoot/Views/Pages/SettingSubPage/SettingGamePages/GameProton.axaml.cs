using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface;
using BedrockBoot.Proton;
using BedrockBoot.Views.Control.Items.Proton;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingGamePages;

public partial class GameProton : ISettingPage
{
    public GameProton()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Game.Breadcrumb.Root"],
                ItemClickAction = s => MainSettingPage.NavigateTo(new SettingGame())
            },
            new()
            {
                ItemName = "Proton"
            }
        };

        UpdateUI();
    }

    private void UpdateUI()
    {
        ListBox.Children.Clear();

        var lst = ProtonCore.GetInstalledVersions();
        if (lst != null)
        {
            lst.ToList().ForEach(l=>ListBox.Children.Add(new InstalledProtonItem(l, UpdateUI)));
        }
    }

    private void InstallProtonBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}