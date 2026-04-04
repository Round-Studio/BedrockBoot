using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using OnePointUI.Avalonia.Styling.Controls.OnePointControls.WindowFrame;

namespace BedrockBoot.Views.Windows;

public partial class ExceptionWindow : OnePointWindow
{
    private I18nManager i18n => I18nManager.Instance;

    public ExceptionWindow()
    {
        InitializeComponent();
    }

    public ExceptionWindow(ErrorReport logs) : this()
    {
        Log = logs;
        LogBox.Text = logs.Exception.ToString();
    }

    public ErrorReport Log { get; set; }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(Log.Exception.ToString());
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveBtnButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = I18nManager.Instance["ExceptionWindow.SaveDialog.Title"],
            SuggestedFileName = Path.GetFileName(Log.FileName),
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(I18nManager.Instance["ExceptionWindow.SaveDialog.FileType"])
                {
                    Patterns = new[] { "*.json" }
                }
            },
            ShowOverwritePrompt = true
        });

        if (file != null)
        {
            var filePath = file.Path.LocalPath;
            File.WriteAllText(filePath, Log.ToJson());
        }
    }

    private async void UploadBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // 禁用按钮防止重复点击
        if (sender is Button btn1) btn1.IsEnabled = false;

        try 
        {
            // 这里的 Log.Exception.ToString() 或 Log.ToJson() 是你要上传的内容
            string logContent = Log.Exception.ToString(); 
        
            string? url = await UploadLogToMclogs(logContent);

            if (!string.IsNullOrEmpty(url))
            {
                // 成功后自动复制到剪贴板
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(url);
                }
            
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        }
        finally
        {
            if (sender is Button btn2) btn2.IsEnabled = true;
        }
    }

    public async Task<string?> UploadLogToMclogs(string content)
    {
        using var client = new HttpClient();
    
        // 设置 User-Agent（mclo.gs 建议带上应用名称和版本）
        client.DefaultRequestHeaders.Add("User-Agent", $"BedrockBoot2/{GlobalModel.BodyVersion}");

        var payload = new
        {
            content = content
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.mclo.gs/1/log", httpContent);
        
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
            
                // 返回格式通常为 {"success": true, "url": "https://mclo.gs/XXXXXXX", "id": "XXXXXXX"}
                if (doc.RootElement.TryGetProperty("url", out var urlElement))
                {
                    return urlElement.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            // 这里可以处理网络异常
            System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
        }

        return null;
    }
}