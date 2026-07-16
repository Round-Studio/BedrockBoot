using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;

namespace BedrockBoot.Views.DrawContent;

public partial class DrawAddWidgetContent : UserControl
{
    public DrawAddWidgetContent()
    {
        InitializeComponent();
        
        DesktopWorkspace.RegistedWidgets.ForEach(wid =>
        {
            Previewer.Children.Add(new WidgetPreviewer(wid));
        });
    }
}