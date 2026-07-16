using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Models.Global;

namespace BedrockBoot.Views.Control.Widgets.DesktopWidgets;

public partial class WidgetPreviewer : UserControl
{
    private readonly WidgetRegisterInfo _wid;

    public WidgetPreviewer()
    {
        InitializeComponent();
    }

    public WidgetPreviewer(WidgetRegisterInfo wid) : this()
    {
        _wid = wid;
        var content = DesktopWorkspace.CreateWidgetFromType(wid.Type);
        WidgetName.Text = wid.Name;
        WidgetDescription.Text = wid.Description;
        ContentControl.Content = content;
    }

    private void AddBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        GlobalModel.MainWindow.CloseDraw();
        DesktopWorkspace.Instance.AddWidget(_wid.Type);
    }
}