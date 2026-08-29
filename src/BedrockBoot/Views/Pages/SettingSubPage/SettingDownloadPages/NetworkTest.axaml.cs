using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Interface;
using BedrockBoot.Models.Global;
using BedrockBoot.Views.Pages.MainSubPage;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;

namespace BedrockBoot.Views.Pages.SettingSubPage.SettingDownloadPages;

public partial class NetworkTest : ISettingPage
{
    private readonly List<NetworkTestEntry> _entries = new();

    public NetworkTest()
    {
        InitializeComponent();

        BreadcrumbItem = new List<BreadcrumbItemInfo>
        {
            new()
            {
                ItemName = I18nManager.Instance["Setting.Download.Breadcrumb.Root"],
                ItemClickAction = _ => MainSettingPage.NavigateTo(new SettingDownload())
            },
            new()
            {
                ItemName = I18nManager.Instance["Settings.Download.NetworkTest.Title"]
            }
        };

        BuildEntries();
        BuildUI();
    }

    private void BuildEntries()
    {
        var mojang = I18nManager.Instance["Settings.Download.NetworkTest.Group.Mojang"];
        var version = I18nManager.Instance["Settings.Download.NetworkTest.Group.Version"];
        var curseForge = I18nManager.Instance["Settings.Download.NetworkTest.Group.CurseForge"];
        var update = I18nManager.Instance["Settings.Download.NetworkTest.Group.Update"];
        var gameFile = I18nManager.Instance["Settings.Download.NetworkTest.Group.GameFile"];
        var plugin = I18nManager.Instance["Settings.Download.NetworkTest.Group.Plugin"];
        var other = I18nManager.Instance["Settings.Download.NetworkTest.Group.Other"];

        // Mojang
        _entries.Add(new NetworkTestEntry(mojang, "Mojang Launcher Content", SourceList.MojangHost));
        _entries.Add(new NetworkTestEntry(mojang, "Mojang Bedrock Patch Notes", SourceList.NewsUrl));

        // Version Sources
        foreach (var src in SourceList.VersionDataSources)
            _entries.Add(new NetworkTestEntry(version, src.Key, src.Value));

        // CurseForge Sources
        foreach (var src in SourceList.CurseForgeSource)
            _entries.Add(new NetworkTestEntry(curseForge, src.Key, src.Value));

        // Update Download Sources（含占位符 {url} 的源用统一探针 URL 替换）
        const string probeRouterUrl = "https://github.com/microsoft/vscode/releases";
        foreach (var src in SourceList.UpdateDownloadSources)
        {
            var url = src.Value.Replace("{url}", probeRouterUrl);
            _entries.Add(new NetworkTestEntry(update, src.Key, url));
        }

        // Game File Download Sources（占位符 {router} 替换为根路径）
        foreach (var src in SourceList.GameFileDownloadSource)
            _entries.Add(new NetworkTestEntry(gameFile, src.Host, src.Url.Replace("{router}", "/")));

        // Plugin / Market
        _entries.Add(new NetworkTestEntry(plugin, "Round Studio Market API", SourceList.MarketApiHost));
        _entries.Add(new NetworkTestEntry(plugin, "Round Studio Plugin API", SourceList.PluginApi));

        // 其他
        _entries.Add(new NetworkTestEntry(other, "VC++ 2015-2022 Redist", SourceList.VC20152022Url));
        _entries.Add(new NetworkTestEntry(other, "GitHub API", "https://api.github.com"));
        _entries.Add(new NetworkTestEntry(other, "EasyTier Public Node", "https://et-public-node.roundstudio.top/"));
    }

    private void BuildUI()
    {
        GroupsHost.Children.Clear();

        var grouped = _entries
            .GroupBy(e => e.Category)
            .ToList();

        foreach (var group in grouped)
        {
            GroupsHost.Children.Add(new TextBlock
            {
                Margin = new Avalonia.Thickness(5, 8, 0, 0),
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                Text = group.Key
            });

            foreach (var entry in group)
            {
                var pingBox = new LabelBox
                {
                    Text = "-",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromArgb(0x66, 0x80, 0x80, 0x80))
                };
                entry.PingBox = pingBox;

                var card = new SettingCard
                {
                    Margin = new Avalonia.Thickness(5, 0, 5, 0),
                    Header = entry.Name,
                    Description = entry.Url,
                    Glyph = "\uE839"
                };
                card.Content = pingBox;

                GroupsHost.Children.Add(card);
            }
        }
    }

    private async void TestAllBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        TestAllBtn.IsEnabled = false;
        TestAllBtn.Content = new ProgressRing
        {
            Width = 20,
            Height = 20,
            Background = Brushes.Transparent
        };

        try
        {
            var tasks = _entries.Select(TestEntryAsync).ToArray();
            await Task.WhenAll(tasks);
        }
        finally
        {
            TestAllBtn.Content = I18nManager.Instance["Settings.Download.NetworkTest.Action.Test"];
            TestAllBtn.IsEnabled = true;
        }
    }

    private static async Task TestEntryAsync(NetworkTestEntry entry)
    {
        if (entry.PingBox == null) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            entry.PingBox.Text = I18nManager.Instance["Settings.Download.NetworkTest.Status.Testing"];
            entry.PingBox.Background = Brushes.Orange;
        });

        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BedrockBoot.NetworkTest");

            var stopwatch = Stopwatch.StartNew();

            using var request = new HttpRequestMessage(HttpMethod.Get, entry.Url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.PingBox.Text = $"{elapsed} ms";
                entry.PingBox.Background = GetLatencyBrush(elapsed, response.IsSuccessStatusCode);
            });
        }
        catch (TaskCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.PingBox.Text = I18nManager.Instance["Settings.Download.NetworkTest.Status.Timeout"];
                entry.PingBox.Background = Brushes.DarkRed;
            });
        }
        catch (Exception)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                entry.PingBox.Text = I18nManager.Instance["Settings.Download.NetworkTest.Status.Failed"];
                entry.PingBox.Background = Brushes.DarkRed;
            });
        }
    }

    private static IBrush GetLatencyBrush(long ms, bool isSuccess)
    {
        if (!isSuccess) return Brushes.OrangeRed;
        if (ms < 200) return Brushes.Green;
        if (ms < 500) return Brushes.Olive;
        if (ms < 1000) return Brushes.Orange;
        if (ms < 3000) return Brushes.OrangeRed;
        return Brushes.DarkRed;
    }

    private sealed class NetworkTestEntry
    {
        public NetworkTestEntry(string category, string name, string url)
        {
            Category = category;
            Name = name;
            Url = url;
        }

        public string Category { get; }
        public string Name { get; }
        public string Url { get; }
        public LabelBox? PingBox { get; set; }
    }
}