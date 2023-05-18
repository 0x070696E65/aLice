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
            var queryString = _uri.Split('?').LastOrDefault();
            if (queryString == null) return;
            var dict = queryString.Split('&')
                .Select(s => s.Split('='))
                .ToDictionary(a => a[0], a => a[1]);
            var hasType = dict.TryGetValue("type", out var type);
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
            
            if (!hasType) throw new NullReferenceException("type is null");
            if (!hasCallbackUrl) throw new NullReferenceException("callback url is null");
            
            if(!hasMethod) method = "get";
            var hasRedirectUrl = dict.TryGetValue("redirect_url", out var redirectUrl);
            
            if (Current?.MainPage != null && requestType == RequestType.Pugkey)
            {
                await Current.MainPage.Navigation.PushModalAsync(new RequestGetPubkey(callbackUrl));
                return;
            }
            if (Current?.MainPage != null && requestType == RequestType.Batches)
            {
                var count = 0;
                var metalList = new List<string>();
                while (true)
                {
                    var hasMetal = dict.TryGetValue("batch" + count, out var metal);
                    if (!hasMetal)
                    {
                        break;
                    }
                    metalList.Add(metal);
                    count++;
                }
                
                if (hasRedirectUrl)
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSignBatches(metalList, callbackUrl, method, redirectUrl));
                }
                else
                {
                    await Current.MainPage.Navigation.PushModalAsync(new RequestSignBatches(metalList, callbackUrl, method));
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
}