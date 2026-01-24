using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Server;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceServer : ISetting
{
    private ServerManager _serverManager;
    private string _key = "";
    private int SelIndex => UserChooseBox.SelectedIndex;
    public VersionConfig VersionConfig { get; set; }
    public InstanceServer()
    {
        InitializeComponent();
    }

    public InstanceServer(VersionConfig versionConfig) : this()
    {
        VersionConfig = versionConfig;
        _serverManager = new ServerManager(VersionConfig);
        UpdateUI();
    }

    public void UpdateUI()
    {
        IsEdit = false;
        UserChooseBox.Items.Clear();
        _serverManager.GetServers().ToList().ForEach(user =>
            {
                UserChooseBox.Items.Add(new ComboBoxItem()
                {
                    Content = user.Key,
                    Tag = user.Value
                });
            }
        );

        if (_serverManager.GetServers().Count >= 1)
        {
            UserChooseBox.SelectedIndex = 0;
            UpdateServer(_serverManager.GetServers().Keys.First());
        }

        IsEdit = true;
    }

    public void UpdateServer(string user)
    {
        NullBox.IsVisible = false;
        ResultBox.Children.Clear();
        
        var servers = _serverManager.GetServers()[user];
        var res = servers
            .Where(x => x.ServerName.Contains(_key) ||
                        x.ServerAddress.Contains(_key) ||
                        x.ServerPort.ToString().Contains(_key))
            .ToList();

        res.ForEach(s => ResultBox.Children.Add(new GameServerItem(s)
        {
            DeleteServer = (info) =>
            {
                _serverManager.DeleteServer(user, info);
                UpdateServer(user);
            }
        }));

        if (res.Count <= 0)
            NullBox.IsVisible = true;
    }

    private void AddServerBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var body = new DialogAddGameServerContent();
        DialogHost.Show(new DialogInfo()
        {
            Title = "添加第三方服务器",
            Content = body,
            CloseButtonText = "添加此",
            PrimaryButtonText = "取消",
            CloseAction = () =>
            {
                _serverManager.AddServer(_serverManager.GetServers().Keys.ToList()[UserChooseBox.SelectedIndex],
                    body.ServerItemInfo);

                UpdateServer(_serverManager.GetServers().Keys.ToList()[UserChooseBox.SelectedIndex]);
            }
        });
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit)
        {
            _key = string.IsNullOrEmpty(SearchBox.Text) ? "" : SearchBox.Text;
            UpdateServer(_serverManager.GetServers().ToList()[SelIndex].Key);
        }
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit)
        {
            UpdateServer(_serverManager.GetServers().ToList()[SelIndex].Key);
        }
    }
}