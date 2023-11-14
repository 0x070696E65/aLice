using aLice.Models;
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
    }
    
    public static async void RequestNotification(string _uri, CancellationToken token)
    {
        try
        {
            await AccountViewModel.SetAccounts();
            if (AccountViewModel.Accounts.accounts.Count == 0)
            {
                throw new NullReferenceException("アカウントが登録されていません");
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
        }
    }
    
    static private async Task NotificationError(string message)
    {
        if (Current?.MainPage != null)
            await Current.MainPage.DisplayAlert("Error", $"必要な情報が不足しています、遷移元開発者にお問い合わせください\n{message}", "閉じる");
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
                    await Current.MainPage.DisplayAlert("確認", baseUrl + "は未登録です。\n使用可能として登録しますか？", "はい", "いいえ");
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
        try
        {
            var p = SecureStorage.GetAsync("CurrentPassword").Result.Split("_");
            var memoryPasswordSeconds = int.Parse(SecureStorage.GetAsync("MemoryPasswordSeconds").Result);
            if (long.Parse(p[1]) +memoryPasswordSeconds < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                SecureStorage.Remove("CurrentPassword");
            }
        }
        catch
        {
            // 念のため削除
            SecureStorage.Remove("CurrentPassword");
        }
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