using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Standard.Entity.Account.Microsoft;
using BedrockBoot.Standard.Interface;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingAccountPages;

public partial class AccountXbox : ISettingPage
{
    private readonly MsUserConfig _msUserConfig;

    public AccountXbox()
    {
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "账户与档案",
                ItemClickAction = (i) =>
                    MainSettingPage.NavigateTo(new SettingAccount())
            },
            new()
            {
                ItemName = "Xbox 账户"
            }
        };
    }

    public AccountXbox(MsUserConfig msUserConfig)
    {
        _msUserConfig = msUserConfig;
        InitializeComponent();
        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = "账户与档案",
                ItemClickAction = (i) =>
                    MainSettingPage.NavigateTo(new SettingAccount())
            },
            new()
            {
                ItemName = _msUserConfig.UserName
            }
        };

        AccountName.Text = _msUserConfig.UserName;
        AccountImage.Update(_msUserConfig.UserIconUrl);
    }

    private void DeleteAccountBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogHost.Show(new()
        {
            Title = "您确定要删除账户吗",
            Content = "删除此账户后，将会丢失该账户的验证凭证，将无法进行第三方服务器的游玩。",
            CloseButtonText = "确定",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                MsAccountManager.AccountConfigEntity!.Data.Accounts.RemoveAt(
                    MsAccountManager.Accounts!.Accounts.FindIndex(x => x.BUID == _msUserConfig.BUID));
                MsAccountManager.AccountConfigEntity!.Save();
                MainSettingPage.NavigateTo(new SettingAccount());
            },
            AccountButton = DialogButtons.CloseButton
        });
    }
}