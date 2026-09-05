using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Interface.Download;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Views.Pages.DownloadPage.ResultSubPage;

public partial class ResultDescription : UserControl
{
    private readonly IDownloadResult _service;
    public Action? NotFountDescription;

    public ResultDescription()
    {
        InitializeComponent();
    }

    public ResultDescription(IDownloadResult service) : this()
    {
        _service = service;
        UpdateUI();
    }

    private async Task UpdateUI()
    {
        PreviewList.Children.Clear();
        if (_service.SearchInfo.Images is { Count: > 0 })
        {
            PreviewCard.IsVisible = true;
            foreach (var image in _service.SearchInfo.Images)
                PreviewList.Children.Add(new LocalImageRenderWidget(image) { Width = 290 });
        }

        var controls = await _service.DescriptionControls();
        if (controls == null || controls.Count <= 0)
        {
            NotFountDescription?.Invoke();
            return;
        }

        ;
        DescControls.Children.AddRange(controls);
        DescCard.IsVisible = controls.Count > 0;
    }
}