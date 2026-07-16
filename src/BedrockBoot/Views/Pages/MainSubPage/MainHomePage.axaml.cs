using System.IO;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Config;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Global;
using BedrockBoot.Style.Widgets;
using BedrockBoot.Views.Control.Widgets;
using BedrockBoot.Views.Control.Widgets.DesktopWidgets;
using BedrockBoot.Views.DrawContent;
using GlobalModel = BedrockBoot.Core.Global.GlobalModel;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainHomePage : BedrockBootPage
{
    public DesktopWorkspace DesktopWorkspace;

    public MainHomePage()
    {
        InitializeComponent();
        this.Loaded += (s, e) => UpdateHome();
    }

    public void UpdateHome()
    {
        MainGrid.Children.Clear();
        DesktopWorkspace = new DesktopWorkspace();
        if (File.Exists(PathsList.WidgetsConfigPath))
            DesktopWorkspace.ImportLayout(File.ReadAllText(PathsList.WidgetsConfigPath));
        DesktopWorkspace.AddWidgetCallOn += (sender, args) =>
        {
            // DesktopWorkspace.AddWidget(WidgetType.Timer);
            Models.Global.GlobalModel.MainWindow.OpenDraw(new DrawAddWidgetContent(), "添加小组件");
        };
        DesktopWorkspace.LayoutChanged += (sender, args) =>
        {
            var json = DesktopWorkspace.ExportLayout();
            File.WriteAllText(PathsList.WidgetsConfigPath, json);
        };

        switch (GlobalModel.Config.Data.HomeConfig.HomeType)
        {
            case HomeType.None:
                break;
            case HomeType.News:
                MainGrid.Children.Add(new GameUpdateNewsWidget());
                break;
            case HomeType.Widgets:
                MainGrid.Children.Add(DesktopWorkspace);
                break;
        }
    }
}