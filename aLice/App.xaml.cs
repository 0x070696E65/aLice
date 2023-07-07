using CatSdk.Utils;

namespace aLice;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // 強制的にライトモードを適用
        if (Application.Current != null) Application.Current.UserAppTheme = AppTheme.Light;

        MainPage = new MainPage();
    }
    
    public static async void RequestNotification(string _uri, CancellationToken token)
    {
        
        try
        {
            /*
            var uri_a = new Uri(_uri);
            var baseUrl_a = $"{uri_a.Scheme}://{uri_a.Authority}";
            Console.WriteLine(baseUrl_a);
            */
            
            var queryString = _uri.Split('?').LastOrDefault();
            if (queryString == null) return;
            var dict = queryString.Split('&')
                .Select(s => s.Split('='))
                .ToDictionary(a => a[0], a => a[1]);
            var hasType = dict.TryGetValue("type", out var type);

            if (!hasType)
            {
                await NotificationError("type is null");
                return;
            }
            
            var requestType = type switch
            {
                "request_sign_utf8" => RequestType.SignUtf8,
                "request_sign_transaction" => RequestType.SignTransaction,
                "request_sign_binary_hex" => RequestType.SignBinaryHex,
                "request_pubkey" => RequestType.Pugkey,
                "request_sign_batches" => RequestType.Batches,
                _ => throw new Exception("type is invalid")
            };
            var hasData = dict.TryGetValue("data", out var data);
            var hasCallbackUrl = dict.TryGetValue("callback", out var callbackUrl);
            var hasMethod = dict.TryGetValue("method", out var method);
            
            if (!hasData)
            {
                await NotificationError("data is null");
                return;
            }

            if (!hasCallbackUrl)
            {
                await NotificationError("callback url is null");
                return;
            }
            
            callbackUrl = Converter.HexToUtf8(callbackUrl);
            
            if(!hasMethod) method = "get";
            var hasRedirectUrl = dict.TryGetValue("redirect_url", out var redirectUrl);
            if (hasRedirectUrl) {
                redirectUrl = Converter.HexToUtf8(redirectUrl);   
            }
            
            var uri = new Uri(callbackUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Authority}";

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
                        return;
                    }
                }
            }

            if (Current?.MainPage != null && requestType == RequestType.Pugkey)
            {
                await Current.MainPage.Navigation.PushModalAsync(new RequestGetPubkey(callbackUrl));
                return;
            }
            if (Current?.MainPage != null && requestType == RequestType.Batches)
            {
                var count = 0;
                var batches = new List<string>();
                while (true)
                {
                    var hasBatches = dict.TryGetValue("batch" + count, out var metal);
                    if (!hasBatches)
                    {
                        break;
                    }
                    batches.Add(metal);
                    count++;
                }
                
                if (hasRedirectUrl)
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSignBatches(batches, callbackUrl, method, redirectUrl));
                }
                else
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSignBatches(batches, callbackUrl, method));
                }
                return;
            }

            if (Current?.MainPage != null && hasData)
            {
                if (hasRedirectUrl)
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSign(data, callbackUrl, requestType, method, redirectUrl));
                }
                else
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSign(data, callbackUrl, requestType, method));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 非同期操作がキャンセルされたときの処理（必要に応じて）
        }
    }

    static private async Task NotificationError(string message)
    {
        if (Current?.MainPage != null)
            await Current.MainPage.DisplayAlert("Error", $"必要な情報が不足しています、遷移元開発者にお問い合わせください\n{message}", "閉じる");
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