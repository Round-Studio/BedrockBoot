using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Pack.Game.Server;
using BedrockBoot.Views.Control;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceServer : ISetting
{
    private ServerManager _serverManager;
    private string _key = "";
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
            .Where(x => x.ServerName.Contains(_key))
            .Where(x => x.ServerAddress.Contains(_key))
            .Where(x => x.ServerPort.ToString().Contains(_key))
            .ToList();
        
        res.ForEach(s => ResultBox.Children.Add(new GameServerItem(s)));

        if (res.Count <= 0)
            NullBox.IsVisible = true;
    }
}