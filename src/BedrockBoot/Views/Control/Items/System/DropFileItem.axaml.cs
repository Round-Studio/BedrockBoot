using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PeNet;

namespace BedrockBoot.Views.Control.Items.System;

public partial class DropFileItem : UserControl
{
    private readonly IStorageItem _file;

    public DropFileItem()
    {
        InitializeComponent();
    }

    public DropFileItem(IStorageItem file) : this()
    {
        _file = file;
        UpdateUi();
    }

    public void UpdateUi()
    {
        var file = _file.Path.LocalPath;
        Card.Header = Path.GetFileName(file);

        var fileType = file.ToLower() switch
        {
            { } s when s.EndsWith(".zip") => "压缩文件",
            { } s when s.EndsWith(".exe") => "Windows 可执行文件",
            { } s when s.EndsWith(".dll") => "动态链接库",
            { } s when s.EndsWith(".mcpack") => "资源包",
            { } s when s.EndsWith(".mcaddon") => "资源包",
            { } s when s.EndsWith(".mcworld") => "存档包",
            _ => "不支持的文件"
        };

        Card.Description = fileType;
        
        if (file.ToLower()
            .EndsWith(".exe"))
        {
            GetExeFileIconAsync();
        }
    }

    private async Task GetExeFileIconAsync()
    {
        try
        {
            byte[]? iconBytes = await Task.Run(() =>
            {
                using var fs = new FileStream(_file.Path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var peFile = new PeFile(fs);
                var icon = peFile.Icons().FirstOrDefault();
                return icon?.AsSpan().ToArray();
            });

            if (iconBytes == null || iconBytes.Length == 0)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var stream = new MemoryStream(iconBytes);
                var bitmap = new Bitmap(stream);
                Card.IsFontIcon = false;
                Card.ImageIcon = bitmap;
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Card.IsFontIcon = true;
                Card.ImageIcon = null;
            });
        }
    }
}