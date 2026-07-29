using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;
using BedrockBoot.Views.Control.Items;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.View;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainAccountPage : BedrockBootPage
{
    public MainAccountPage()
    {
        InitializeComponent();
        
        UpdateUi();
    }

    public void UpdateUi()
    {
        IsEdit = false;
        var users = MsAccountManager.Accounts?.Accounts;
        UsersList.IsVisible = false;
        UsersList.Items.Clear();
        NoneCard.IsVisible = true;
        
        if (users != null)
        {
            NoneCard.IsVisible = users.Count == 0;
            if(users.Count == 0) return;
            users.ForEach(user =>
            {
                UsersList.Items.Add(new ItemViewItem()
                {
                    Content = new XboxUserItem(user)
                });
            });
            if (string.IsNullOrEmpty(MsAccountManager.Accounts?.SelectUserBuid))
            {
                MsAccountManager.AccountConfigEntity?.Data.SelectUserBuid = users[0].BUID;
                MsAccountManager.AccountConfigEntity?.Save();
            }
            var selIndex = users.FindLastIndex(user => user.BUID == MsAccountManager.Accounts?.SelectUserBuid);
            UsersList.SelectedIndex = selIndex;
            UsersList.IsVisible = true;
        }

        IsEdit = true;
    }

    private async void LoginBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        await MsAccountManager.LoginAccount();
        UpdateUi();
    }

    private void UsersList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            MsAccountManager.AccountConfigEntity?.Data.SelectUserBuid =
                MsAccountManager.Accounts?.Accounts[UsersList.SelectedIndex].BUID;
            MsAccountManager.AccountConfigEntity?.Save();
        }
    }
}