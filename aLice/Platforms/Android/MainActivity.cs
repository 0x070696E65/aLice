using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace aLice;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

[IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.ActionView,
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = "alice", DataPathPrefix = "sign"
    )
]

public class MainActivity : MauiAppCompatActivity
{
    private CancellationTokenSource _cts;
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Window != null)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.White);
            Window.SetNavigationBarColor(Android.Graphics.Color.White);
        }

        var uri = Intent?.Data;
        if (uri == null) return;
        _cts = new CancellationTokenSource();
        App.RequestNotification(uri.ToString(), _cts.Token);
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        var strLink = intent.DataString;
        if (strLink == null) return;
        
        _cts = new CancellationTokenSource();
        App.RequestNotification(strLink, _cts.Token);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _cts?.Cancel();
    }
}

