using System.Globalization;
using aLice.Models;
using aLice.Resources;
using aLice.ViewModels;
using aLice.Views;

namespace aLice;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // 強制的にライトモードを適用
        if (Current != null) Current.UserAppTheme = AppTheme.Light;

        MainPage = new MainPage();
        
        // 言語設定
        var language = SecureStorage.GetAsync("Language").Result;
        switch (language)
        {
            case "ja":
                CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
                CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
                break;
            case "en":
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");
                break;
            default:
                CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
                CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
                break;
        }
    }
    
    public static async void RequestNotification(string _uri, CancellationToken token)
    {
        try
        {
            await AccountViewModel.SetAccounts();
            if (AccountViewModel.Accounts.accounts.Count == 0)
            {
                throw new NullReferenceException(AppResources.App_NoAccount);
            }
            
            var notification = new NotificationModel(_uri);
            RequestViewModel.SetNotification(notification);

            if (notification.BaseUrl != null)
            {
                await ApproveDomain(notification.BaseUrl, notification.CallbackUrl);
            }

            if (Current?.MainPage == null) return;
            
            if(notification.RequestType == RequestType.Pubkey)
            {
                await Current.MainPage.Navigation.PushModalAsync(
                    new RequestGetPubkey());
                return;
            }

            if (notification.RequestType == RequestType.Batches)
            {
                await Current.MainPage.Navigation.PushModalAsync(
                    new RequestSignBatches());
                return;
            }
            
            if (notification.Data != null)
            {
                await Current.MainPage.Navigation.PushModalAsync(
                    new RequestSign()
                    );
            }
        }
        catch (Exception e)
        {
            await NotificationError(e.Message);
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
    
    static private async Task NotificationError(string message)
    {
        if (Current?.MainPage != null)
            await Current.MainPage.DisplayAlert("Error", $"{AppResources.App_NotificationError}\n{message}", AppResources.LangUtil_Close);
    }

    static private async Task ApproveDomain(string baseUrl, string callbackUrl)
    {
        var domains = Array.Empty<string>();
        try
        {
            domains = (await SecureStorage.GetAsync("domains")).Split(',');
        }
        catch
        {
            // ignored
        }

        if (!domains.Contains(baseUrl))
        {
            if (Current?.MainPage != null)
            {
                var isRegistDomain =
                    await Current.MainPage.DisplayAlert("Confirm", string.Format(AppResources.App_RegisterDomain, baseUrl), AppResources.LangUtil_Yes, AppResources.LangUtil_No);
                if (isRegistDomain)
                {
                    var addedDomains = domains.Append(baseUrl);
                    await SecureStorage.SetAsync("domains", string.Join(",", addedDomains));
                }
                else
                {
                    RejectedRequestSign(callbackUrl);
                }
            }
        }
    }
    
    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Created += (sender, args) =>
        {
            AccountViewModel.DeletePasswordByTimestamp();
        };
        return window;
    }
    
    static private async void RejectedRequestSign(string callbackUrl)
    {
        const string additionalParam = "error=sign_rejected";
        if (callbackUrl.Contains('?')) {
            callbackUrl += "&" + additionalParam;
        }
        else {
            callbackUrl += "?" + additionalParam;
        }
        await Launcher.OpenAsync(new Uri(callbackUrl));
    }
}