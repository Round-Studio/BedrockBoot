using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BedrockBoot.Base.Enum.Type;
using BedrockBoot.Models.Pack.System.DropFile;
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

        var fileType = DropFileCheck.GetFileTypeName(DropFileCheck.CheckFile(file));

        Card.Description = fileType;
        
        if (DropFileCheck.CheckFile(file) == DropFileType.Exe)
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