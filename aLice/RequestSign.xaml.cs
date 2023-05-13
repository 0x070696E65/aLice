using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class RequestSign : ContentPage
{
    private readonly string data;
    private readonly string callbackUrl;
    private readonly string type;
    private SavedAccount mainAccount;
    public RequestSign(string _uri)
    {
        InitializeComponent();
        var queryString = _uri.Split('?').LastOrDefault();
        if (queryString == null) return;
        var dict = queryString.Split('&')
            .Select(s => s.Split('='))
            .ToDictionary(a => a[0], a => a[1]);
        data = dict["data"];
        callbackUrl = dict["callback"];
        type = dict.TryGetValue("type", out var value) ? value : "hex";
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ShowRequestSign();
    }

    // 署名を要求されたときに呼び出される
    private async Task ShowRequestSign()
    {
        try
        {
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            if (savedAccounts.accounts[0] == null) throw new NullReferenceException("アカウントが登録されていません");
            mainAccount = savedAccounts.accounts.Find((acc) => acc.isMain);
            try
            {
                Transaction.Text = SymbolTransaction.ParseTransaction(data);
            }
            catch(Exception e)
            {
                await Console.Error.WriteLineAsync(e.Message);
                Transaction.Text = "これはSymbolのトランザクションではありません";
                Data.Text = data;
            }
            Ask.Text = $"{mainAccount.accountName}で署名しますか？";
        }
        catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestSign(object sender, EventArgs e)
    {
        Error.Text = "";
        try
        {
            var password = await DisplayPromptAsync("Password", "パスワードを入力してください", "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
            if (password == null) return;
            string privateKey;
            try {
                privateKey = CatSdk.Crypto.Crypto.DecryptString(mainAccount.encryptedPrivateKey, password, mainAccount.address);
            }
            catch {
                throw new Exception("パスワードが正しくありません");
            }

            var keyPair = new KeyPair(new PrivateKey(privateKey));
            var b = type switch
            {
                "hex" => Converter.HexToBytes(data),
                "utf8" => System.Text.Encoding.UTF8.GetBytes(data),
                _ => throw new Exception("typeが不正です")
            };
            var signature = keyPair.Sign(b);

            var systemKeyPair = new KeyPair(new PrivateKey(Env.PRIVATE_KEY));
            var hash = systemKeyPair.Sign(signature.bytes);
            
            await Launcher.OpenAsync(new Uri($"{callbackUrl}?sig={Converter.BytesToHex(signature.bytes)}&hash={Converter.BytesToHex(hash.bytes)}"));
            await Navigation.PopModalAsync();
        } catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    // 署名を拒否したときに呼び出される
    private async void RejectedRequestSign(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Launcher.OpenAsync(new Uri($"{callbackUrl}?error=sign_rejected"));
    }
}