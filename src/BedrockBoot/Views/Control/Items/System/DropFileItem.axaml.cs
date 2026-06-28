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

        var fileType = DropFileCheck.GetFileTypeName(file);

        Card.Description = fileType;
    }
}