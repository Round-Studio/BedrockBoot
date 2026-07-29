using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;

namespace BedrockBoot.Views.Control.Items;

public partial class XboxUserItem : UserControl
{
    public XboxUserItem()
    {
        InitializeComponent();
    }

    public XboxUserItem(MsUserConfig user) : this()
    {
        UserName.Text = user.UserName;
        UserLoginTime.Text = user.AuthResult.SavedAt.ToString();
        _ = UserIcon.Update(user.UserIconUrl);
    }
}