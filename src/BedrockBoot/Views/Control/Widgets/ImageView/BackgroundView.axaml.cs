using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BedrockBoot.Base.Entry;
using BedrockBoot.Base.Enum;
using BedrockBoot.Models.Style.Background.AnimationImage;

namespace BedrockBoot.Views.Control.Widgets.ImageView;

public partial class BackgroundView : UserControl
{
    private Thread AnimationThread { get; set; }
    private CancellationTokenSource _cts;
    private CancellationTokenSource _rotationCts;
    private const double Radius = 400;
    private const double PeriodSeconds = 10;
    private readonly object _lock = new object();
    private bool _isAnimationMode;
    private bool _isRotating;
    private double _currentRotation;
    private Random _random = new Random();
    
    public BackgroundView()
    {
        InitializeComponent();
        _cts = new CancellationTokenSource();
        _rotationCts = new CancellationTokenSource();
        CreateAnimationThread();
    }
    
    private void CreateAnimationThread()
    {
        var token = _cts.Token;
        AnimationThread = new Thread(() =>
        {
            var startTime = DateTime.Now;
            
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var angle = (elapsed / PeriodSeconds) * 2 * Math.PI;
                    var x = Radius * Math.Cos(angle);
                    var y = Radius * Math.Sin(angle);
                    
                    if (token.IsCancellationRequested) break;
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        lock (_lock)
                        {
                            Margin = new Thickness(x, y, -x, -y);
                        }
                    });
                    
                    Thread.Sleep(16);
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
    }
    
    public async void ApplyImageBackground(StyleConfig style)
    {
        var imgPath = style.BackgroundImage;
        if (!File.Exists(imgPath)) return;
        
        StopRotationAnimation();
        ResetAllProperties();
        
        SetBackgroundBlur(0);
        StopAnimation();

        try
        {
            BackgroundImage.IsVisible = false;
            BackgroundImage3D.IsVisible = false;
            _isAnimationMode = false;
            _currentRotation = 0;
            _isRotating = false;

            if (!style.BackgroundAnimation)
            {
                var bitmap = LoadScaledByFactorOptimized(imgPath,
                    Core.Global.GlobalModel.Config.Data.StyleConfig.ImageQuality switch
                    {
                        ImageQuality.High => 1,
                        ImageQuality.Medium => 0.3,
                        ImageQuality.Lower => 0.1
                    }, Core.Global.GlobalModel.Config.Data.StyleConfig.ImageQuality switch
                    {
                        ImageQuality.High => BitmapInterpolationMode.HighQuality,
                        ImageQuality.Medium => BitmapInterpolationMode.LowQuality,
                        ImageQuality.Lower => BitmapInterpolationMode.LowQuality
                    });
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
                SetBackgroundBlur(style.BackgroundImageBlur);
            }
            else
            {
                _isAnimationMode = true;
                var animHelper = new AnimationImageHelper(imgPath);
                BackgroundImage.IsVisible = true;
                BackgroundImage.Background = new ImageBrush
                {
                    Stretch = Stretch.UniformToFill,
                    Source = await animHelper.GetImage()
                };
                
                if (BackgroundBox.RenderTransform is not TransformGroup)
                {
                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform());
                    transformGroup.Children.Add(new RotateTransform());
                    BackgroundBox.RenderTransform = transformGroup;
                    BackgroundBox.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                }
                else
                {
                    foreach (var transform in ((TransformGroup)BackgroundBox.RenderTransform).Children)
                    {
                        if (transform is RotateTransform rotate)
                        {
                            rotate.Angle = 0;
                        }
                        if (transform is ScaleTransform scale)
                        {
                            scale.ScaleX = 1;
                            scale.ScaleY = 1;
                        }
                    }
                }
                
                _rotationCts = new CancellationTokenSource();
                ApplyAnimationBlur(200);
                
                StartRotationAnimation();
                StartAnimation();
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    
    private void ResetAllProperties()
    {
        Margin = new Thickness(0);
        _currentRotation = 0;
        _isRotating = false;
        _isAnimationMode = false;
        
        if (BackgroundBox.RenderTransform is TransformGroup transformGroup)
        {
            foreach (var transform in transformGroup.Children)
            {
                if (transform is RotateTransform rotate)
                {
                    rotate.Angle = 0;
                }
                if (transform is ScaleTransform scale)
                {
                    scale.ScaleX = 1;
                    scale.ScaleY = 1;
                }
            }
        }
        
        BackgroundBox.Effect = null;
        BackgroundBox.Margin = new Thickness(0);
        BackgroundBox.ClipToBounds = true;
        
        BackgroundImage.IsVisible = false;
        BackgroundImage3D.IsVisible = false;
        BackgroundImage.Background = null;
        BackgroundImage3D.Source = null;
        
        BackgroundImageOpacity.Opacity = 1;
    }
    
    private void StopRotationAnimation()
    {
        if (_rotationCts != null && !_rotationCts.IsCancellationRequested)
        {
            _rotationCts.Cancel();
        }
        _isRotating = false;
    }
    
    private async void StartRotationAnimation()
    {
        var token = _rotationCts.Token;
        var duration = 20;
        var angle = 360;
        var targetRotation = _currentRotation + angle;
        var startRotation = _currentRotation;
        var startTime = DateTime.Now;
        var totalMilliseconds = duration * 1000;
        var isFirstRun = true;
        
        while (_isAnimationMode && !token.IsCancellationRequested)
        {
            if (!_isRotating)
            {
                if (!isFirstRun)
                {
                    duration = _random.Next(5, 21);
                    angle = _random.Next(90, 361);
                    targetRotation = _currentRotation + angle;
                    startRotation = _currentRotation;
                    startTime = DateTime.Now;
                    totalMilliseconds = duration * 1000;
                }
                
                _isRotating = true;
                isFirstRun = false;
            }
            
            if (token.IsCancellationRequested) break;
            
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / totalMilliseconds, 1);
            
            var easeProgress = progress < 0.5 
                ? 2 * progress * progress 
                : 1 - Math.Pow(-2 * progress + 2, 2) / 2;
            
            _currentRotation = startRotation + (targetRotation - startRotation) * easeProgress;
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    if (!token.IsCancellationRequested && BackgroundBox.RenderTransform is TransformGroup transformGroup)
                    {
                        foreach (var transform in transformGroup.Children)
                        {
                            if (transform is RotateTransform rotate)
                            {
                                rotate.Angle = _currentRotation;
                            }
                        }
                    }
                }
            });
            
            if (progress >= 1)
            {
                _currentRotation = targetRotation;
                _isRotating = false;
            }
            
            await Task.Delay(16, token).ContinueWith(t => { });
        }
    }
    
    private void ApplyAnimationBlur(int radius)
    {
        if (radius > 0)
        {
            if (BackgroundBox.Effect is not BlurEffect blur)
            {
                blur = new BlurEffect();
                BackgroundBox.Effect = blur;
            }

            blur.Radius = radius;
            
            var scale = 1 + (radius / 150.0);
            if (BackgroundBox.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var transform in transformGroup.Children)
                {
                    if (transform is ScaleTransform scaleTransform)
                    {
                        scaleTransform.ScaleX = scale;
                        scaleTransform.ScaleY = scale;
                    }
                }
            }
            
            BackgroundBox.Margin = new Thickness(-radius);
            BackgroundBox.ClipToBounds = false;
        }
        else
        {
            BackgroundBox.Effect = null;
            if (BackgroundBox.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var transform in transformGroup.Children)
                {
                    if (transform is ScaleTransform scaleTransform)
                    {
                        scaleTransform.ScaleX = 1;
                        scaleTransform.ScaleY = 1;
                    }
                }
            }
            BackgroundBox.Margin = new Thickness(0);
            BackgroundBox.ClipToBounds = true;
        }
        
        BackgroundImageOpacity.Opacity =
            (100 - Core.Global.GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }
    
    private void StartAnimation()
    {
        if (AnimationThread == null || !AnimationThread.IsAlive)
        {
            if (AnimationThread != null && !AnimationThread.IsAlive)
            {
                _cts.Dispose();
                _cts = new CancellationTokenSource();
                CreateAnimationThread();
            }
            AnimationThread.Start();
        }
    }

    private void StopAnimation()
    {
        _isAnimationMode = false;
        _isRotating = false;
        
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
        
        if (AnimationThread != null && AnimationThread.IsAlive)
        {
            AnimationThread.Join(500);
        }
    }

    public Bitmap LoadScaledByFactorOptimized(string filePath, double scale, BitmapInterpolationMode quality = BitmapInterpolationMode.LowQuality)
    {
        using var stream = File.OpenRead(filePath);
        var imageInfo = SixLabors.ImageSharp.Image.Identify(stream);
        int originalWidth = imageInfo.Width;
        int originalHeight = imageInfo.Height;
        int targetWidth = (int)(originalWidth * scale);
        if (targetWidth < 1) targetWidth = 1;

        stream.Seek(0, SeekOrigin.Begin);
        return Bitmap.DecodeToWidth(stream, targetWidth, BitmapInterpolationMode.LowQuality);
    }
    
    private void SetBackgroundBlur(int radius)
    {
        if (_isAnimationMode)
        {
            ApplyAnimationBlur(radius);
            return;
        }
        
        if (radius > 0)
        {
            if (BackgroundBox.Effect is not BlurEffect blur)
            {
                blur = new BlurEffect();
                BackgroundBox.Effect = blur;
            }

            blur.Radius = radius;
            BackgroundBox.Margin = new Thickness(-radius);
            BackgroundBox.ClipToBounds = false;
        }
        else
        {
            BackgroundBox.Effect = null;
            BackgroundBox.Margin = new Thickness(0);
            BackgroundBox.ClipToBounds = true;
        }
        
        BackgroundImageOpacity.Opacity =
            (100 - Core.Global.GlobalModel.Config.Data.StyleConfig.BackgroundImageOpacity) * 0.01;
    }
    
    public void Dispose()
    {
        StopAnimation();
        StopRotationAnimation();
        _cts?.Dispose();
        _rotationCts?.Dispose();
        AnimationThread?.Join(1000);
    }
}