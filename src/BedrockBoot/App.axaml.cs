using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Base.Enum.Language;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Style;
using BedrockBoot.ViewModels;
using BedrockBoot.Views.Windows;
using OnePointUI.Avalonia.Style.Core;
using Round.SDK.Entity;
using Application = Avalonia.Application;
using ResourceDictionary = Avalonia.Controls.ResourceDictionary;

namespace BedrockBoot;

public class App : Application
{
    public override void Initialize()
    {
        if (GlobalModel.Config == null)
        {
            GlobalModel.Config = new ConfigEntity<ConfigEntry>(PathsList.ConfigPath);
            GlobalModel.Config.Load();
        }

        ServicePointManager.DefaultConnectionLimit = 1024;

        ThemeManager.Initialize(this);
        AvaloniaXamlLoader.Load(this);
        
        I18nManager.Instance.SystemLanguage(GlobalModel.Config.Data.Language);

        // 订阅所有全局异常处理器
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;

        Console.WriteLine(@"异常订阅已完毕");
        LoadColor();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogException(exception, "AppDomain");
        ShowErrorDialog(exception);

        // 非致命错误可以继续运行
        if (!e.IsTerminating)
        {
        }
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception, "Task");
        ShowErrorDialog(e.Exception);
        e.SetObserved(); // 标记为已处理
    }

    private void ShowErrorDialog(Exception ex)
    {
        try
        {
            if(!Directory.Exists(PathsList.ReportPath)) Directory.CreateDirectory(PathsList.ReportPath);
            var errorReportJson = ErrorReport.Create(GlobalModel.Config.Data, $"错误报告", ex);
            errorReportJson.SaveToFile(Path.Combine(PathsList.ReportPath,
                DateTime.Now.ToString("yyyyMMddHHmmss") + ".json"));
            Dispatcher.UIThread.Invoke(() => { new ExceptionWindow(errorReportJson).Show(); });
        }
        catch (Exception dialogEx)
        {
            // 如果对话框本身出错，至少记录到控制台
            Console.WriteLine($@"显示错误对话框时出错: {dialogEx}");
        }
    }

    private void LogException(Exception ex, string source)
    {
        Console.WriteLine($@"{source} 异常: {ex?.ToString() ?? "未知异常"}");
    }

    private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "UI Thread");
        ShowErrorDialog(e.Exception);
        e.Handled = true; // 阻止应用崩溃
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }

    public static void LoadColor()
    {
        try
        {
            ThemeManager.Instance.SetAccentColor(
                Color.Parse(AccentColor.Colors[GlobalModel.Config.Data.StyleConfig.AccentColorIndex]));
            ThemeManager.Instance.SetThemeModel(
                GlobalModel.Config.Data.StyleConfig.LightThemeType == ThemeModelEnum.Light
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark);
        }
        catch
        {
        }
    }
}