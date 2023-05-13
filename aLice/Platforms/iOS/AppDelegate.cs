using Foundation;
using UIKit;

namespace aLice;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private CancellationTokenSource _cts;
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (userActivity.ActivityType != NSUserActivityType.BrowsingWeb) return false;
        if (userActivity.WebPageUrl == null) return true;
        var url = userActivity.WebPageUrl.AbsoluteString;
        _cts = new CancellationTokenSource();
        App.RequestSignatureForNotification(url, _cts.Token);
        return true;
    }
    
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        _cts = new CancellationTokenSource();
        App.RequestSignatureForNotification(url.ToString(), _cts.Token);
        return true;
    }
}