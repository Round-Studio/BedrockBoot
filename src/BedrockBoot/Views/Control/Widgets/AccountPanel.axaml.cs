using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Views.Control.Items.Xbox;
using BedrockBoot.Views.Pages;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Control.Widgets;

public partial class AccountPanel : UserControl
{
    private bool IsEdit = false;
    public AccountPanel()
    {
        InitializeComponent();
        UpdateAccount();
    }

    public async Task UpdateAccount()
    {
        IsEdit = false;
        if (Core.Global.GlobalModel.Config.Data.IsUseMultipleUsers)
        {
            var users = MsAccountManager.Accounts?.Accounts;
            var selIndex = users.FindLastIndex(user => user.BUID == MsAccountManager.Accounts?.SelectUserBUID);

            AccountList.Items.Clear();
            users.ForEach(user =>
            {
                AccountList.Items.Add(new ItemViewItem
                {
                    Width = 208,
                    Content = new AccountSmallItem(user)
                });
            });
            AccountList.SelectedIndex = selIndex;
        }

        IsEdit = true;
    }

    private async void AccountList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            MsAccountManager.AccountConfigEntity?.Data.SelectUserBUID =
                MsAccountManager.Accounts?.Accounts[AccountList.SelectedIndex].BUID;
            MsAccountManager.AccountConfigEntity?.Save();

            await Models.Global.GlobalModel.MainWindow.UpdateAccount(true);
        }
    }

    private void GoToAccountPage_OnClick(object? sender, RoutedEventArgs e)
    {
        MainPage.Instance.SelTag.SelectedIndex = 5;
    }
}