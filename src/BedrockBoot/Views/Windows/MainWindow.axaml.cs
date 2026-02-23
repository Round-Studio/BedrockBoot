using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Entry.Manifest;
using BedrockBoot.Base.Enum;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Service;
using BedrockBoot.Service.Protocol;
using BedrockBoot.Views.Pages;
using BedrockBoot.Views.Pages.SetupPage;
using BedrockBoot.Views.Windows.SubWindows;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : BedrockBootWindow
{
    private I18nManager I18n => I18nManager.Instance;

    public MainWindow()
    {
        GlobalModel.MainWindow = this;
        InitializeComponent();
        
        UpdateBack();
        
        // 1. 初始化窗口几何信息
        InitializeWindowBounds();
        
        // 2. 绑定任务列表更新回调 (使用 Post 确保线程安全)
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Post(UpdateTaskUI);
        
        // 3. 注册全局快捷键
        SetupDynamicHotkey();
        
        // 4. 执行异步初始化流程
        _ = InitializeAsync();
    }

    #region 初始化流程

    private void InitializeWindowBounds()
    {
        var winInfo = GlobalModel.Config.Data.WindowInfo;
        if (winInfo.X != -1 && winInfo.Y != -1)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(winInfo.X, winInfo.Y);
            Width = winInfo.Width;
            Height = winInfo.Height;
        }

#if DEBUG
        DebugModule.IsVisible = true;
        VersionBox.IsVisible = false;
#endif

        MainFrame.NavigateTo(new LoadingPage());
        VersionBox.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        var buildTimestamp = (DateTime)CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly());
        BuildTime.Text = $"Build.2.{buildTimestamp:yy.MMdd.HHmmss}";
        
        if (!Directory.Exists(PathsList.TempPath)) 
            Directory.CreateDirectory(PathsList.TempPath);
    }

    private async Task InitializeAsync()
    {
        // 加载功能配置文件
        try
        {
            GlobalModel.FunctionOption = await new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>("avares://BedrockBoot/Manifest/Function/FunctionOption.json");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load FunctionOption: {ex.Message}");
        }

        // 注册文件关联
        HandleFileAssociations();

        // 核心引擎异步初始化
        await InitBedrockCoreAsync();

        // 完成初始化后回到 UI 线程进行页面跳转
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (GlobalModel.Config.Data.IsFirstRun)
                MainFrame.NavigateTo(new SetupRoot());
            else
                MainFrame.NavigateTo(new MainPage());

            CheckUserAgreement();
        });
    }

    private void HandleFileAssociations()
    {
#if RELEASE
        if (GlobalModel.FunctionOption?.IsEnableMcPackOpenWithBody == true)
            OpenAgreement.RegisterAssociation();
#else
        OpenAgreement.RegisterAssociation();
#endif
    }

    private async Task InitBedrockCoreAsync()
    {
        try
        {
            GlobalModel.BedrockCore = new BedrockCore
            {
                Options = new CoreOptions
                {
                    IsAutoCompleteVC = true,
                    IsAutoOpenDevelopment = true,
                    IsAutoCompleteGameInput = true,
                    IsCheckMD5 = true
                }
            };
            await GlobalModel.BedrockCore.InitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"BedrockCore Init Error: {ex}");
            
            if (ex.Message.Contains("Not Support Windows Version"))
            {
                await Dispatcher.UIThread.InvokeAsync(() => DialogHost.Show(new DialogInfo
                {
                    Title = I18n["MainWindow.Dialog.UnsupportedSys.Title"],
                    Content = I18n["MainWindow.Dialog.UnsupportedSys.Content"],
                    CloseButtonText = I18n["MainWindow.Dialog.UnsupportedSys.Close"],
                    CloseAction = () => Environment.Exit(1)
                }));
            }
        }
    }

    private void CheckUserAgreement()
    {
        if (GlobalModel.Config.Data.IsAgreeTerms) return;

        DialogHost.Show(new DialogInfo
        {
            Content = I18n["MainWindow.Dialog.Agreement.Content"],
            Title = I18n["MainWindow.Dialog.Agreement.Title"],
            CloseButtonText = I18n["MainWindow.Dialog.Agreement.Agree"],
            CloseAction = () =>
            {
                GlobalModel.Config.Data.IsAgreeTerms = true;
                GlobalModel.Config.Save();
            },
            PrimaryButtonText = I18n["MainWindow.Dialog.Agreement.Decline"],
            PrimaryAction = () => Environment.Exit(0),
            AccountButton = DialogButtons.CloseButton
        });
    }

    #endregion

    #region 视觉渲染 (背景处理)

    public void UpdateBack()
    {
        // 状态重置
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        AnimationBackground.IsVisible = false;

        var style = GlobalModel.Config.Data.StyleConfig;

        switch (style.StyleType)
        {
            case StyleType.Mica:
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica };
                break;
            case StyleType.Blur:
                TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };
                break;
            case StyleType.Image:
                ApplyImageBackground(style);
                break;
            case StyleType.AccentColor:
                AccentBackgroundBox.IsVisible = true;
                AccentBackgroundBox.Opacity = 0.7;
                break;
            case StyleType.Voronoi:
                AnimationBackground.IsVisible = true;
                AnimationBackground.BackgroundType = BackgroundType.Voronoi;
                break;
            case StyleType.Bubble:
                AnimationBackground.IsVisible = true;
                AnimationBackground.BackgroundType = BackgroundType.Bubble;
                break;
        }
    }

    private void ApplyImageBackground(StyleConfig style)
    {
        if (style.BackgroundImageSelectedIndex == -1 || style.BackgroundImages.Count == 0) return;

        var imgPath = style.BackgroundImages[style.BackgroundImageSelectedIndex];
        if (!File.Exists(imgPath)) return;

        try
        {
            BackgroundBox.IsVisible = true;
            BackgroundImage.IsVisible = false;
            BackgroundImage3D.IsVisible = false;
            
            SetBackgroundBlur(style.BackgroundImageBlur);
            
            var bitmap = new Bitmap(imgPath);
            if (style.Background3D)
            {
                BackgroundImage3D.IsVisible = true;
                BackgroundImage3D.Source = bitmap;
                BackgroundImage3D.Stretch = Stretch.UniformToFill;
            }
            else
            {
                BackgroundImage.IsVisible = true;
                BackgroundImage.Background = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill,
                    Source = bitmap
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Background image render error: {ex.Message}");
        }
    }

    public void SetBackgroundBlur(int radius)
    {
        if (radius > 0)
        {
            BackgroundBox.Effect = new BlurEffect { Radius = radius };
            BackgroundBox.Margin = new Thickness(-radius);
        }
        else
        {
            BackgroundBox.Effect = null;
            BackgroundBox.Margin = new Thickness(0);
        }
        
        // 透明度应用
        BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }

    #endregion

    #region 任务与交互

    public void UpdateTaskUI()
    {
        TaskList.Children.Clear();
        var tasks = GlobalModel.TaskManager.Tasks;

        if (tasks.Count == 0)
        {
            TaskViewer.IsVisible = false;
            NoneBox.IsVisible = true;
            TaskInfoText.IsVisible = false;
        }
        else
        {
            TaskViewer.IsVisible = true;
            NoneBox.IsVisible = false;
            TaskInfoText.IsVisible = true;
            TaskInfoText.Text = string.Format(I18n["MainWindow.Task.CountInfo"], tasks.Count);

            foreach (var task in tasks)
            {
                if (task.Item == null) continue;
                task.Item.Margin = new Thickness(5);
                TaskList.Children.Add(task.Item);
            }
        }
    }

    private void SetupDynamicHotkey()
    {
        this.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl + V 全局粘贴逻辑
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            if (e.Source is not (TextBox or TextBlock))
            {
                CopyService.HandleCopyAction();
                e.Handled = true;
            }
        }
    }

    private void TaskBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsTaskCardOpen) CloseTaskCard();
        else OpenTaskCard();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // 保存窗口状态
        GlobalModel.Config.Data.WindowInfo = new WindowInfo
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            X = Position.X,
            Y = Position.Y
        };
        GlobalModel.Config.Save();
    }

    #endregion
}