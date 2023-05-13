namespace aLice;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();
    }
    
    public static async void RequestSignatureForNotification(string uri, CancellationToken token)
    {
        try
        {
            if (Current?.MainPage != null)
                await Current.MainPage.Navigation.PushModalAsync(new RequestSign(uri));
        }
        catch (OperationCanceledException)
        {
            // 非同期操作がキャンセルされたときの処理（必要に応じて）
        }
    }
}