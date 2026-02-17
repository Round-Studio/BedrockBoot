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
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;
using Round.SDK.Helper;

namespace BedrockBoot.Views.Windows;

public partial class MainWindow : BedrockBootWindow
{
    private I18nManager i18n => I18nManager.Instance;

    public MainWindow()
    {
        GlobalModel.MainWindow = this;
        InitializeComponent();
        GlobalModel.TaskManager.OnChanged = () => Dispatcher.UIThread.Invoke(UpdateTaskUI);
        SetupDynamicHotkey();

        UpdateBack();

        if (!Directory.Exists(PathsList.TempPath)) Directory.CreateDirectory(PathsList.TempPath);

        if (GlobalModel.Config.Data.WindowInfo.X != -1 && GlobalModel.Config.Data.WindowInfo.Y != -1)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(GlobalModel.Config.Data.WindowInfo.X,
                GlobalModel.Config.Data.WindowInfo.Y);

            Width = GlobalModel.Config.Data.WindowInfo.Width;
            Height = GlobalModel.Config.Data.WindowInfo.Height;

            Console.WriteLine(
                $@"Main Window: Width {GlobalModel.Config.Data.WindowInfo.Width}, Height {GlobalModel.Config.Data.WindowInfo.Height}");
        }

#if DEBUG
        DebugModule.IsVisible = true;
        VersionBox.IsVisible = false;
#endif

        MainFrame.NavigateTo(new LoadingPage());
        VersionBox.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        BuildTime.Text =
            $"Build.2.{((DateTime)CheckVersion.GetBuildTimestamp(Assembly.GetExecutingAssembly())).ToString("yy.MMdd.HHmmss")}";
        
        Task.Run(async () =>
        {
            GlobalModel.FunctionOption = await new JsonResourceEntity()
                .LoadJsonResourceAsync<FunctionOptionEntry>(
                    "avares://BedrockBoot/Manifest/Function/FunctionOption.json");

#if RELEASE
            if (GlobalModel.FunctionOption.IsEnableMcPackOpenWithBody)
                OpenAgreement.RegisterAssociation();
#else
            OpenAgreement.RegisterAssociation();
#endif

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
                Console.WriteLine(ex);
                if (ex.Message.Contains("Not Support Windows Version"))
                    DialogHost.Show(new DialogInfo
                    {
                        Title = I18nManager.Instance["MainWindow.Dialog.UnsupportedSys.Title"],
                        Content = I18nManager.Instance["MainWindow.Dialog.UnsupportedSys.Content"],
                        CloseButtonText = I18nManager.Instance["MainWindow.Dialog.UnsupportedSys.Close"],
                        CloseAction = () => Environment.Exit(1)
                    });
            }

            try
            {
                // OpenProtocol();
            }
            catch (InvalidOperationException)
            {
                DialogHost.Show(new DialogInfo
                {
                    Title = I18nManager.Instance["MainWindow.Dialog.NoNetwork.Title"],
                    Content = I18nManager.Instance["MainWindow.Dialog.NoNetwork.Content"],
                    CloseButtonText = I18nManager.Instance["MainWindow.Dialog.NoNetwork.Close"]
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Error: {ex}");

                if (!GlobalModel.BedrockCore.GetWindowsDevelopmentState())
                {
                    DialogHost.Show(new DialogInfo
                    {
                        Title = I18nManager.Instance["MainWindow.Dialog.DevMode.Title"],
                        Content = I18nManager.Instance["MainWindow.Dialog.DevMode.Content"],
                        CloseButtonText = I18nManager.Instance["MainWindow.Common.Confirm"]
                    });
                }
            }
            finally
            {
                if (GlobalModel.Config.Data.IsFirstRun)
                {
                    Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new SetupRoot()));
                }
                else
                {
                    Dispatcher.UIThread.Invoke(() => MainFrame.NavigateTo(new MainPage()));
                }
                
                if (!GlobalModel.Config.Data.IsAgreeTerms)
                    DialogHost.Show(new DialogInfo
                    {
                        Content = I18nManager.Instance["MainWindow.Dialog.Agreement.Content"],
                        Title = I18nManager.Instance["MainWindow.Dialog.Agreement.Title"],
                        CloseButtonText = I18nManager.Instance["MainWindow.Dialog.Agreement.Agree"],
                        CloseAction = () =>
                        {
                            GlobalModel.Config.Data.IsAgreeTerms = true;
                            GlobalModel.Config.Save();
                        },
                        PrimaryButtonText = I18nManager.Instance["MainWindow.Dialog.Agreement.Decline"],
                        PrimaryAction = () => { Environment.Exit(0); },
                        AccountButton = DialogButtons.CloseButton
                    });
            }
        });
    }

    public void OpenProtocol()
    {
        var pro = new ProtocolRegister();
        pro.ProtocolName = "BedrockBoot";
        pro.ProtocolDescription = I18nManager.Instance["MainWindow.Protocol.Description"];
        pro.RegisterProtocol(Process.GetCurrentProcess().MainModule.FileName);

        GlobalModel.ProtocolService.StartAsync();
        GlobalModel.ProtocolService.Get("/shell", async (context, parameters) =>
        {
            parameters.TryGetQuery("command", out var command);
            var comm = command.Replace("bedrockboot://", "").Split('/');
            ProtocolCommand.OnCommand(comm);
            await ProtocolService.WriteResponseAsync(context, 200, "ok");
        });
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        GlobalModel.Config.Data.WindowInfo = new WindowInfo
        {
            Width = Bounds.Width,
            Height = Bounds.Height,
            X = Position.X,
            Y = Position.Y
        };
        GlobalModel.Config.Save();
    }

    public async Task UpdateBack()
    {
        TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.Transparent };
        BackgroundBox.IsVisible = false;
        AccentBackgroundBox.IsVisible = false;
        AnimationBackground.IsVisible = false;
        
        if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Mica)
        {
            TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.Mica };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Blur)
        {
            TransparencyLevelHint = new List<WindowTransparencyLevel> { WindowTransparencyLevel.AcrylicBlur };
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Image)
        {
            BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
            var index = GlobalModel.Config.Data.StyleConfig.BackgroundImageSelectedIndex;
            if (index != -1 && GlobalModel.Config.Data.StyleConfig.BackgroundImages.Count > 0)
            {
                BackgroundBox.IsVisible = true;
                BackgroundImage.IsVisible = false;
                BackgroundImage3D.IsVisible = false;
                SetBackgroundBlur(GlobalModel.Config.Data.StyleConfig.BackgroundImageBlur);

                var imgPath = GlobalModel.Config.Data.StyleConfig.BackgroundImages[index];
                if (GlobalModel.Config.Data.StyleConfig.Background3D)
                {
                    BackgroundImage3D.IsVisible = true;
                    BackgroundImage3D.Source = new Bitmap(imgPath);
                    BackgroundImage3D.Stretch = Stretch.UniformToFill;
                }
                else
                {
                    BackgroundImage.IsVisible = true;
                    BackgroundImage.Background = new ImageBrush
                    {
                        Stretch = Stretch.UniformToFill,
                        Source = new Bitmap(imgPath)
                    };
                }
            }
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.AccentColor)
        {
            AccentBackgroundBox.IsVisible = true;
            AccentBackgroundBox.Opacity = 0.7;
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Voronoi)
        {
            AnimationBackground.IsVisible = true;
            AnimationBackground.BackgroundType = BackgroundType.Voronoi;
        }
        else if (GlobalModel.Config.Data.StyleConfig.StyleType == StyleType.Bubble)
        {
            AnimationBackground.IsVisible = true;
            AnimationBackground.BackgroundType = BackgroundType.Bubble;
        }
    }

    public void SetBackgroundBlur(int num)
    {
        if (num != 0)
        {
            BackgroundBox.Effect = new BlurEffect { Radius = num };
            BackgroundBox.Margin = new Thickness(-num);
        }
        else
        {
            BackgroundBox.Effect = null;
            BackgroundBox.Margin = new Thickness(0);
        }
        BackgroundImageOpacity.Opacity = (100 - GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }
    
    private void SetupDynamicHotkey()
    {
        this.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private async Task HandlePasteAsync()
    {
        CopyService.HandleCopyAction();
    }
    
    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            var source = e.Source;
            if (source is not (TextBox or TextBlock))
            {
                await HandlePasteAsync();
                e.Handled = true;
            }
        }
    }

    private void TaskBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsTaskCardOpen) CloseTaskCard();
        else OpenTaskCard();
    }

    public void UpdateTaskUI()
    {
        TaskList.Children.Clear();
        var taskCount = GlobalModel.TaskManager.Tasks.Count;

        if (taskCount <= 0)
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
            TaskInfoText.Text = string.Format(I18nManager.Instance["MainWindow.Task.CountInfo"], taskCount);

            GlobalModel.TaskManager.Tasks.ForEach(task =>
            {
                task.Item.Margin = new Thickness(5);
                TaskList.Children.Add(task.Item);
            });
        }
    }
}