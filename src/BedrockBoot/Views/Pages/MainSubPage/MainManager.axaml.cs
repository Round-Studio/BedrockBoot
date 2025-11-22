using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BedrockBoot.Base.Entry;
using BedrockBoot.Models.Global;
using BedrockBoot.Models.Helper;
using BedrockBoot.Views.Control;
using BedrockBoot.Views.DialogContent;
using OnePointUI.Avalonia.Base.Entry;
using OnePointUI.Avalonia.Base.Enum;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Dialog;

namespace BedrockBoot.Views.Pages.MainSubPage;

public partial class MainManager : BedrockBootPage
{
    public bool IsEditMode { get; set; } = false;
    private FileSystemWatcher _configWatcher;

    public MainManager()
    {
        InitializeComponent();
        
        UpdateUI();
    }

    private void InitializeConfigWatcher()
    {
        // 先清理现有的监听器
        CleanupConfigWatcher();

        try
        {
            if (GlobalModel.Config.Data.GameFolders.Count == 0)
                return;

            // 获取当前选中的游戏文件夹路径
            var currentFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
            string gameFolderPath = currentFolder.GameFolderPath;
            
            if (!Directory.Exists(gameFolderPath))
                return;

            // 创建 FileSystemWatcher
            _configWatcher = new FileSystemWatcher
            {
                Path = gameFolderPath,
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true // 监听子目录
            };
            
            // 注册事件处理
            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Deleted += OnConfigFileChanged;
            _configWatcher.Created += OnConfigFileChanged;
            _configWatcher.Renamed += OnConfigFileChanged;
            
            // 开始监听
            _configWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化配置文件监听失败: {ex.Message}");
        }
    }

    private void CleanupConfigWatcher()
    {
        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Changed -= OnConfigFileChanged;
            _configWatcher.Dispose();
            _configWatcher = null;
        }
    }

    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // 由于文件可能被锁定，需要重试机制
        await Task.Delay(100);
        
        try
        {
            // 只在文件在 bedrock_versions 目录或其子目录中时刷新
            if (e.FullPath.Contains("bedrock_versions", StringComparison.OrdinalIgnoreCase))
            {
                // 在主线程中更新UI
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine($"检测到文件变化: {e.ChangeType} - {e.FullPath}");
                    UpdateGameList();
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"配置文件变化处理失败: {ex.Message}");
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        CleanupConfigWatcher();
    }

    public void UpdateUI()
    {
        IsEditMode = false;
        
        if (GlobalModel.Config.Data.GameFolders.Count <= 0)
        {
            FolderList.IsVisible = false;
            FolderNull.IsVisible = true;
        }
        else
        {
            FolderList.IsVisible = true;
            FolderNull.IsVisible = false;
        }

        FolderList.SelectedIndex = -1;
        FolderList.Items.Clear();
        
        GlobalModel.Config.Data.GameFolders.ForEach(folder =>
        {
            FolderList.Items.Add(new ListBoxItem()
            {
                Content = new StackPanel()
                {
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = folder.GameFolderName,
                            Margin = new Thickness(5, 2, 5, 5),
                            FontSize = 16
                        },
                        new TextBlock()
                        {
                            Foreground = Brushes.Gray,
                            Text = folder.GameFolderPath,
                            Margin = new Thickness(5, 0, 5, 0),
                            FontSize = 9,
                            TextWrapping = TextWrapping.WrapWithOverflow
                        }
                    }
                },
                VerticalAlignment = VerticalAlignment.Center
            });
        });
        
        if (GlobalModel.Config.Data.GameFolders.Count == 1)
            FolderList.SelectedIndex = 0;
        else
            FolderList.SelectedIndex = GlobalModel.Config.Data.GameFolderSelIndex;
        
        // 初始化或重新初始化文件监听
        InitializeConfigWatcher();
        
        UpdateGameList();
        
        IsEditMode = true;
    }

    public void UpdateGameList()
    {
        if (GlobalModel.Config.Data.GameFolders.Count == 0)
        {
            GamesNull.IsVisible = true;
            GameScro.IsVisible = false;
            return;
        }

        var currentFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex];
        var versionsPath = Path.Combine(currentFolder.GameFolderPath, "bedrock_versions");
        
        if (!Directory.Exists(versionsPath))
        {
            GamesNull.IsVisible = true;
            GameScro.IsVisible = false;
            return;
        }
        
        var lst = Directory.GetDirectories(versionsPath);
        
        GameList.Children.Clear();

        if (lst.Length > 0)
        {
            GamesNull.IsVisible = false;
            GameScro.IsVisible = true;
        }
        else
        {
            GamesNull.IsVisible = true;
            GameScro.IsVisible = false;
        }

        lst.ToList().ForEach(x =>
        {
            try
            {
                var info = GameInfoHelper.GetVersionConfig(x);
                Console.WriteLine($"读取到实例：{info.Info.VersionName} : {info.Info.Version}");

                GameList.Children.Add(new GameItem(info));
            }
            catch
            {
                // 忽略加载失败的版本
            }
        });
    }

    private void AddFolderBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new DialogAddGameFolderContent();
         
        DialogHost.Show(new DialogInfo()
        {
            Title = "Add Game Folder",
            Content = dialog,
            CloseButtonText = "添加",
            SecondaryButtonText = "取消",
            AccountButton = DialogButtons.CloseButton,
            CloseAction = () =>
            {
                if (Directory.Exists(dialog.FolderPath))
                {
                    var name = string.IsNullOrEmpty(dialog.FolderName)
                        ? Path.GetFileName(Path.GetDirectoryName(dialog.FolderPath))
                        : dialog.FolderName;
                    
                    GlobalModel.Config.Data.GameFolders.Add(new GameFolderInfo()
                    {
                        GameFolderPath = dialog.FolderPath,
                        GameFolderName = name
                    });
                    GlobalModel.Config.Save();
                    
                    UpdateUI();
                }
            }
        });
    }

    private void FolderList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsEditMode && FolderList.SelectedIndex >= 0)
        {
            GlobalModel.Config.Data.GameFolderSelIndex = FolderList.SelectedIndex;
            GlobalModel.Config.Save();
            
            // 当切换文件夹时重新初始化监听器
            InitializeConfigWatcher();
            
            UpdateGameList();
        }
    }
}