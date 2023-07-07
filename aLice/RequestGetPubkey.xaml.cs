using System.Text.Json;

namespace aLice;

public partial class RequestGetPubkey : ContentPage
{
    private SavedAccount mainAccount;
    private string callbackUrl;
    private SavedAccounts savedAccounts;

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
            savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            if (savedAccounts.accounts[0] == null) throw new NullReferenceException("アカウントが登録されていません");
            mainAccount = savedAccounts.accounts.Find((acc) => acc.isMain);
            Ask.Text = $"{mainAccount.accountName}の公開鍵を渡しても良いですか？";
        }
        catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    private async void ChangeAccount(object sender, EventArgs e)
    {
        var accountNames = new string[savedAccounts.accounts.Count];
        for (var i = 0; i < savedAccounts.accounts.Count; i++)
        {
            accountNames[i] = savedAccounts.accounts[i].accountName;
        }
        var accName = await DisplayActionSheet("アカウント切り替え", "cancel", null, accountNames);
        mainAccount = savedAccounts.accounts.Find(acc => acc.accountName == accName);
        Ask.Text = $"{mainAccount.accountName}の公開鍵を渡しても良いですか？";
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestGetPubkey(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        var additionalParam = $"pubkey={mainAccount.publicKey}";
        if (callbackUrl.Contains('?')) {
            callbackUrl += "&" + additionalParam;
        }
        else {
            callbackUrl += "?" + additionalParam;
        }
        await Launcher.OpenAsync(new Uri(callbackUrl));
    }
    
    // 公開鍵要求を拒否したときに呼び出される
    private async void RejectedRequestGetPubkey(object sender, EventArgs e)
    {
        const string additionalParam = "error=sign_rejected";
        if (callbackUrl.Contains('?')) {
            callbackUrl += "&" + additionalParam;
        }
        else {
            callbackUrl += "?" + additionalParam;
        }
        await Launcher.OpenAsync(new Uri(callbackUrl));
        await Navigation.PopModalAsync();
    }
}