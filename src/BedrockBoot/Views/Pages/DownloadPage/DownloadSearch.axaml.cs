using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;
using BedrockBoot.Views.Pages.DownloadPage.SearchSubPage;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.Navigation;

namespace BedrockBoot.Views.Pages.DownloadPage;

public partial class DownloadSearch : UserControl
{
    public static NavigationFrame SearchFrame;
    public static SearchDetailed SearchDetailed;
    public static DownloadSearch DownloadSearchView;
    
    // 保存搜索状态
    private static string _lastSearchKey = string.Empty;
    private static SearchResourceType _lastSearchType = SearchResourceType.Unknow;
    
    public string SearchKey => KeyBox.Text;
    
    public DownloadSearch()
    {
        InitializeComponent();

        DownloadSearchView = this;
        SearchFrame = NavigationFrame;
        
        // 恢复上次的搜索关键词
        if (!string.IsNullOrEmpty(_lastSearchKey))
        {
            KeyBox.Text = _lastSearchKey;
        }
        
        // 延迟导航，确保UI已加载完成
        Dispatcher.UIThread.Post(() =>
        {
            NavigationFrame.NavigateTo(new SearchDefault());
            
            // 如果有保存的搜索记录，自动导航到详细搜索页面
            if (!string.IsNullOrEmpty(_lastSearchKey))
            {
                if (SearchDetailed == null)
                {
                    NavigationFrame.NavigateTo(new SearchDetailed());
                }
                
                SearchDetailed.OnSearch(new SearchInfo()
                {
                    Key = _lastSearchKey,
                    Type = _lastSearchType
                });
            }
        });
        
        KeyBox.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                SearchBtn_OnClick(null, null);
            }
        };
    }

    private void SearchBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SearchDetailed == null)
        {
            NavigationFrame.NavigateTo(new SearchDetailed());
        }
        
        var searchKey = KeyBox.Text;
        var searchType = Classify(searchKey);
        
        // 保存搜索状态
        _lastSearchKey = searchKey;
        _lastSearchType = searchType;

        SearchDetailed.OnSearch(new SearchInfo()
        {
            Key = searchKey,
            Type = searchType
        });
    }
    
    public SearchResourceType Classify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return SearchResourceType.ResourcePack;
            
        // 情况1：检查是否是版本号格式（支持多种格式）
        if (IsVersionNumber(text))
            return SearchResourceType.Minecraft;
            
        // 情况2：检查是否是带空格的数字序列
        if (IsNumberSequence(text))
            return SearchResourceType.Minecraft;

        return SearchResourceType.ResourcePack;
    }
    
    /// <summary>
    /// 判断是否为版本号格式
    /// </summary>
    private bool IsVersionNumber(string text)
    {
        // 常见的版本号格式：
        // 1. 纯数字版本：1, 1.0, 1.0.0, 1.0.0.0
        // 2. 带字母后缀：1.0a, 1.0-beta, 1.0.0-rc1
        // 3. 带字母前缀：v1.0, V2.1.3
        // 4. 带特殊分隔符：1_0, 1-0
        
        // 移除常见的版本前缀（如v、V）
        string processedText = text.Trim().ToLower();
        if (processedText.StartsWith("v"))
            processedText = processedText.Substring(1);
        
        // 模式1：基本版本号格式（数字.数字.数字...）
        // 例如：1.2.3, 0.1, 2024.12.31
        string versionPattern1 = @"^\d+(?:[\._-]\d+)*$";
        if (Regex.IsMatch(processedText, versionPattern1))
            return true;
            
        // 模式2：带字母或特殊字符的版本号
        // 例如：1.2.3-beta, 2.0a, 1.0-rc1, 3.4.5+build123
        string versionPattern2 = @"^\d+(?:[\._-]\d+)*(?:[\._-]?[a-zA-Z][a-zA-Z0-9]*)?(?:\+[a-zA-Z0-9]+)?$";
        if (Regex.IsMatch(processedText, versionPattern2))
            return true;
            
        return false;
    }
    
    /// <summary>
    /// 判断是否为带空格的数字序列
    /// </summary>
    private bool IsNumberSequence(string text)
    {
        // 模式：一个或多个数字，中间用空格分隔
        // 例如："1 2 3", "123 456 789", "1 2 1 23"
        
        // 去首尾空格
        string trimmedText = text.Trim();
        
        // 如果包含非数字和非空格字符，直接返回false
        foreach (char c in trimmedText)
        {
            if (!char.IsDigit(c) && c != ' ')
                return false;
        }
        
        // 按空格分割
        string[] parts = trimmedText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        // 必须有至少两个数字部分才被认为是"带空格的数字序列"
        if (parts.Length < 2)
            return false;
            
        // 检查每个部分是否都是有效的数字
        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;
                
            // 如果部分不是纯数字，返回false
            if (!IsPureDigits(part))
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查字符串是否只包含数字
    /// </summary>
    private bool IsPureDigits(string text)
    {
        foreach (char c in text)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }
}