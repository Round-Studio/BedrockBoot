using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Ava;

[Application(Label = "Ava", AllowBackup = true, SupportsRtl = true)]
public class AndroidApp : AvaloniaAndroidApplication<BedrockBoot.App>
{
    public AndroidApp(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder).With(new AndroidPlatformOptions
        {
            RenderingMode = new[] { AndroidRenderingMode.Egl, AndroidRenderingMode.Software },
        });
    }
}