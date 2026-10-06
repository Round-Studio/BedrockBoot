using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BedrockBoot.Models.Helper;
using BedrockBoot.Standard.Entity.Account.Microsoft;

namespace BedrockBoot.Views.Control.Items.Xbox;

public partial class AccountSmallItem : UserControl
{
    private readonly MsUserConfig _user;
    private ImageLoader _imageLoader = new ImageLoader();

    public AccountSmallItem()
    {
        InitializeComponent();
    }
    public AccountSmallItem(MsUserConfig user):this()
    {
        _user = user;
        Task.Run(() =>
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                AccountName.Text = user.UserName;
                AccountIcon.Background = new ImageBrush()
                {
                    Source = await _imageLoader.LoadIconAsync(user.UserIconUrl)
                };
            });
        });
    }
}