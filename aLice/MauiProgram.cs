using Microsoft.Extensions.Logging;

namespace aLice;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans_regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans_Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fontawesome_webfont.ttf", "FontAwesome");
                fonts.AddFont("times_new_roman.ttf", "TimesNewRoman");
            });
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}