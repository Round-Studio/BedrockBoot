using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Control.Widgets;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Views.Pages.SettingSubPage;

public partial class SettingAccount : ISettingPage
{
    public SettingAccount()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "账户与档案"
            }
        };

        var users = MsAccountManager.Accounts.Accounts;
        users.ForEach(user => AccountPanel.Children.Add(new AccountCard(user)));
    }
}