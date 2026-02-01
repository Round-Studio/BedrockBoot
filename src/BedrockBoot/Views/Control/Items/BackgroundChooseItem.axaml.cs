using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BedrockBoot.Views.Control.Items;

public partial class BackgroundChooseItem : UserControl
{
    public BackgroundChooseItem()
    {
        InitializeComponent();
    }

    public string ImagePath { get; set; } = string.Empty;

    public void UpdateUI()
    {
        FilePath.Text = ImagePath;
        FileName.Text = Path.GetFileNameWithoutExtension(ImagePath);
        // 方法1：使用 CreateScaledBitmap 并手动计算宽度
        using (var originalBitmap = new Bitmap(ImagePath))
        {
            // 计算等比例缩放后的宽度
            var aspectRatio = originalBitmap.Size.Width / originalBitmap.Size.Height;
            var newWidth = (int)(48 * aspectRatio);

            var resizedBitmap = originalBitmap.CreateScaledBitmap(
                new PixelSize(newWidth, 48)
            );

            ImageBox.Background = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
                Source = resizedBitmap
            };
        }

        ImageBox.Child = null;
    }
}