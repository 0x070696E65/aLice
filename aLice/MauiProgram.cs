using Camera.MAUI;
using Microsoft.Extensions.Logging;

namespace aLice;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCameraView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("opensans_regular.ttf", "OpenSansRegular");
                fonts.AddFont("opensans_semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fontawesome_webfont.ttf", "FontAwesome");
                fonts.AddFont("times_new_roman.ttf", "TimesNewRoman");
            });
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}