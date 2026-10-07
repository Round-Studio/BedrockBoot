using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Standard.Entity.Account.Microsoft;

namespace BedrockBoot.Views.Control.Widgets;

public partial class AccountCard : Button
{
    private readonly MsUserConfig _user;

    public AccountCard()
    {
        InitializeComponent();
    }

    public AccountCard(MsUserConfig user) : this()
    {
        _user = user;
        UpdateUi();
    }

    private void UpdateUi()
    {
        AccountHeader1.Update(_user.UserIconUrl);
        AccountHeader2.Update(_user.UserIconUrl);
        AccountName.Text = _user.UserName;
    }
}