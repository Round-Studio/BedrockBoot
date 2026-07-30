using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Account.Microsoft;
using BedrockBoot.Models.Account.Microsoft;

namespace BedrockBoot.Views.Control.Items;

public partial class XboxUserItem : UserControl
{
    private readonly MsUserConfig _user;
    public Action<string>? OnDelete { get; set; }
    public XboxUserItem()
    {
        InitializeComponent();
    }

    public XboxUserItem(MsUserConfig user) : this()
    {
        _user = user;
        UserName.Text = user.UserName;
        UserLoginTime.Text = user.AuthResult.SavedAt.ToString();
        _ = UserIcon.Update(user.UserIconUrl);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        PART_ControlPanel.Opacity = 1;
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        PART_ControlPanel.Opacity = 0;
    }

    private void DelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        OnDelete?.Invoke(_user.BUID);
    }
}