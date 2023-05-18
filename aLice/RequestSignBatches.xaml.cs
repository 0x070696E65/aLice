using System.Text;
using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class RequestSignBatches : ContentPage
{
    private List<string> data;
    private string callbackUrl;
    private readonly string method;
    private readonly List<string> args;
    private readonly string redirectUrl;
    private SavedAccount mainAccount;
    private readonly List<(IBaseTransaction transaction, string parsedTransaction)> parsedTransaction;
    
    public RequestSignBatches(List<string> _data, string _callbackUrl, string _method, List<string> _args = null, string _redirectUrl = null)
    {
        InitializeComponent();
        data = _data;
        args = _args;
        redirectUrl = _redirectUrl;
        callbackUrl = _callbackUrl;
        method = _method;
        parsedTransaction = new List<(IBaseTransaction transaction, string parsedTransaction)>();
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
            foreach (var s in data)
            {
                var tx = SymbolTransaction.ParseEmbeddedTransaction(s);
                tx.transaction.SignerPublicKey = new PublicKey(Converter.HexToBytes(mainAccount.publicKey));
                Data.Text += tx.parsedTransaction;
                parsedTransaction.Add(tx);
            }
            Type.Text = "複数のトランザクションです";
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
            var network = parsedTransaction[0].transaction.Network == CatSdk.Symbol.NetworkType.MAINNET ? CatSdk.Symbol.Network.MainNet : CatSdk.Symbol.Network.TestNet;
            var metal = new Metal(network);
            var txs = parsedTransaction.Select(valueTuple => valueTuple.transaction).ToList();
            
            var aggs = metal.SignedAggregateCompleteTxBatches(txs, keyPair, network);
            switch (method)
            {
                case "post":
                {
                    var dic = new Dictionary<string, string> {{"pubkey", mainAccount.publicKey}};
                    for (var i = 0; i < aggs.Count; i++)
                    {
                        dic.Add("metal" + i, Converter.BytesToHex(aggs[i].Serialize()));   
                    }
                    for (var i = 0; i < args.Count; i++)
                    {
                        dic.Add("arg" + i, args[i]);   
                    }
                    using var client = new HttpClient();
                    var content = new StringContent(JsonSerializer.Serialize(dic), Encoding.UTF8, "application/json");
                    var response =  client.PostAsync(callbackUrl, content).Result;
                    await response.Content.ReadAsStringAsync();
                    if (redirectUrl != null) await Launcher.OpenAsync(new Uri(redirectUrl));
                    break;
                }
                case "get":
                {
                    var url = $"{callbackUrl}?pubkey={mainAccount.publicKey}&original_data={data}";
                    for (var i = 0; i < aggs.Count; i++)
                    {
                        var signedPayload = Converter.BytesToHex(aggs[i].Serialize());
                        url += $"&signed_{i}={signedPayload}";
                    }
                    for (var i = 0; i < args.Count; i++) {
                        url += $"&args{i}={args[i]}";
                    }
                    await Launcher.OpenAsync(new Uri(url));
                    break;
                }
                default:
                    throw new Exception("不正なリクエストです");
            }

            Reset();
            await Navigation.PopModalAsync();
        } 
        catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    // 署名を拒否したときに呼び出される
    private async void RejectedRequestSign(object sender, EventArgs e)
    {
        await Launcher.OpenAsync(new Uri($"{callbackUrl}?error=sign_rejected"));
        Reset();
        await Navigation.PopModalAsync();
    }

    private void Reset()
    {
        callbackUrl = null;
        data = null;
        mainAccount = null;
    }
}