using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class RequestGetPubkey : ContentPage
{
    private SavedAccount mainAccount;
    private readonly string callbackUrl;
    private string pubkey;

    public RequestGetPubkey(string _callbackUrl)
    {
        InitializeComponent();
        callbackUrl = _callbackUrl;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ShowRequestGetPubkey();
    }

    // 公開鍵を要求されたときに呼び出される
    private async Task ShowRequestGetPubkey()
    {
        try
        {
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            if (savedAccounts.accounts[0] == null) throw new NullReferenceException("アカウントが登録されていません");
            mainAccount = savedAccounts.accounts.Find((acc) => acc.isMain);
            pubkey = mainAccount.publicKey;
            Ask.Text = $"{mainAccount.accountName}の公開鍵を渡しても良いですか？";
        }
        catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestGetPubkey(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Launcher.OpenAsync(new Uri($"{callbackUrl}?pubkey={pubkey}"));
    }
    
    // 公開鍵要求を拒否したときに呼び出される
    private async void RejectedRequestGetPubkey(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Launcher.OpenAsync(new Uri($"{callbackUrl}?error=get_pubkey_rejected"));
    }
}