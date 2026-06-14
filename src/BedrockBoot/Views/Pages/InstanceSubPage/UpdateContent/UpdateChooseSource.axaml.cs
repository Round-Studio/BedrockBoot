using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Views.Control.Items;
using BedrockLauncher.Core;

namespace BedrockBoot.Views.Pages.InstanceSubPage.UpdateContent;

public partial class UpdateChooseSource : UserControl
{
    private readonly List<GameDownloadUrlInfo> _sources;

    public UpdateChooseSource()
    {
        InitializeComponent();
    }

    public UpdateChooseSource(BuildInfo buildInfo, List<GameDownloadUrlInfo> sources) : this()
    {
        _sources = sources;
        BuildInfo = buildInfo;
        UpdateUi();
    }

    public BuildInfo BuildInfo { get; }
    public string? SelectedUrl { get; private set; }

    private async void UpdateUi()
    {
        SourceSelBox.Items.Clear();
        LoadRing.IsVisible = true;

        var hasBestSourceSet = false;
        var itemList = new List<GameDownloadSourceItem>();

        for (var i = 0; i < _sources.Count; i++)
        {
            var urlInfo = _sources[i];
            var item = new GameDownloadSourceItem(urlInfo);
            var currentIndex = i;

            item.Pinged = index =>
            {
                if (!hasBestSourceSet)
                {
                    hasBestSourceSet = true;
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LoadRing.IsVisible = false;
                        SourceSelBox.SelectedIndex = index;
                        SelectedUrl = _sources[index].Url;
                    });
                }
            };

            itemList.Add(item);
            SourceSelBox.Items.Add(new ListBoxItem { Content = item });
        }

        SourceSelBox.SelectionChanged += (_, _) =>
        {
            if (SourceSelBox.SelectedIndex >= 0 && SourceSelBox.SelectedIndex < _sources.Count)
                SelectedUrl = _sources[SourceSelBox.SelectedIndex].Url;
        };

        for (var i = 0; i < itemList.Count; i++)
            itemList[i].OnPing(i);
    }
}
