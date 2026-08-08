using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Models.Style.Background.AnimationImage;

public class AnimationImageHelper : IDisposable
{
    private readonly string _imagePath;
    private ImageLoader _imageLoader =  new ImageLoader();

    public AnimationImageHelper(string imagePath)
    {
        _imagePath = imagePath;
    }
    
    public async Task<Bitmap?> GetImage(int height = 128)
    {
        var bitmap = await _imageLoader.LoadIconAsync(_imagePath);
        if (bitmap == null)
            return null;
    
        var originalWidth = bitmap.PixelSize.Width;
        var originalHeight = bitmap.PixelSize.Height;
    
        var newWidth = (int)((double)originalWidth * height / originalHeight);
    
        var resizedBitmap = bitmap.CreateScaledBitmap(
            new PixelSize(newWidth, height),
            BitmapInterpolationMode.HighQuality);
    
        bitmap.Dispose();
    
        return resizedBitmap;
    }

    public void Dispose()
    {
        _imageLoader.Dispose();
    }
}