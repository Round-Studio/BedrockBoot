using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Pack.Game.Server;
using BedrockBoot.Views.Control.Items;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.InstanceSubPage.DrawContent;

public partial class InstanceServer : ISetting
{
    private static I18nManager i18n => I18nManager.Instance;
    private readonly ServerManager? _serverManager;
    private string _searchKey = string.Empty;

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

    private int SelIndex => UserChooseBox.SelectedIndex;
    public VersionConfig VersionConfig { get; set; }

    /// <summary>
    /// 初始化 UI 并扫描所有用户的服务器配置文件
    /// </summary>
    public void UpdateUI()
    {
        if (_serverManager == null) return;
        
        IsEdit = false;
        UserChooseBox.Items.Clear();

        var servers = _serverManager.GetServers();
        foreach (var user in servers)
        {
            UserChooseBox.Items.Add(new ComboBoxItem
            {
                Content = user.Key,
                Tag = user.Value
            });
        }

        if (servers.Count >= 1)
        {
            UserChooseBox.SelectedIndex = 0;
            UpdateServer(servers.Keys.First());
        }

        IsEdit = true;
    }

    /// <summary>
    /// 根据当前选中的用户和搜索关键词更新服务器列表
    /// </summary>
    public void UpdateServer(string userKey)
    {
        if (_serverManager == null) return;

        NullBox.IsVisible = false;
        ResultBox.Children.Clear();

        var serverDict = _serverManager.GetServers();
        if (!serverDict.TryGetValue(userKey, out var servers))
        {
            NullBox.IsVisible = true;
            return;
        }

        // 过滤逻辑
        var filteredList = servers
            .Where(x => string.IsNullOrEmpty(_searchKey) ||
                        x.ServerName.Contains(_searchKey, StringComparison.OrdinalIgnoreCase) ||
                        x.ServerAddress.Contains(_searchKey, StringComparison.OrdinalIgnoreCase) ||
                        x.ServerPort.ToString().Contains(_searchKey))
            .ToList();

        foreach (var s in filteredList)
        {
            ResultBox.Children.Add(new GameServerItem(s)
            {
                DeleteServer = info =>
                {
                    _serverManager.DeleteServer(userKey, info);
                    UpdateServer(userKey);
                }
            });
        }

        if (filteredList.Count <= 0)
            NullBox.IsVisible = true;
    }

    /// <summary>
    /// 弹出对话框添加第三方服务器
    /// </summary>
    private void AddServerBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_serverManager == null || SelIndex < 0) return;

        var body = new DialogAddGameServerContent();
        DialogHost.Show(new DialogInfo
        {
            Title = i18n["Instance.Server.Add.Title"],
            Content = body,
            CloseButtonText = i18n["Instance.Server.Add.Action"],
            PrimaryButtonText = i18n["MainWindow.Common.Cancel"],
            CloseAction = () =>
            {
                // 安全获取当前选中的用户 Key
                var userKeys = _serverManager.GetServers().Keys.ToList();
                if (SelIndex < userKeys.Count)
                {
                    var currentUser = userKeys[SelIndex];
                    _serverManager.AddServer(currentUser, body.ServerItemInfo);
                    UpdateServer(currentUser);
                }
            }
        });
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (IsEdit && _serverManager != null)
        {
            _searchKey = SearchBox.Text ?? string.Empty;
            
            var userKeys = _serverManager.GetServers().Keys.ToList();
            if (SelIndex >= 0 && SelIndex < userKeys.Count)
            {
                UpdateServer(userKeys[SelIndex]);
            }
        }
    }

    private void UserChooseBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEdit && _serverManager != null)
        {
            var userKeys = _serverManager.GetServers().Keys.ToList();
            if (SelIndex >= 0 && SelIndex < userKeys.Count)
            {
                UpdateServer(userKeys[SelIndex]);
            }
        }
    }
}