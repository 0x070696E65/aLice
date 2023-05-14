using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class RequestSign : ContentPage
{
    private string data;
    private string callbackUrl;
    private RequestType type;
    private byte[] bytesData;
    private SavedAccount mainAccount;
    private (ITransaction transaction, string parsedTransaction) parsedTransaction;

    public RequestSign(string _data, string _callbackUrl, RequestType _type)
    {
        InitializeComponent();
        data = _data;
        callbackUrl = _callbackUrl;
        type = _type;
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
            if(type == RequestType.SignUtf8)
            {
                bytesData = System.Text.Encoding.UTF8.GetBytes(data);
                Type.Text = "UTF8文字列です";
                Data.Text = data;
            } else if(type == RequestType.SignBinaryHex)
            {
                bytesData = Converter.HexToBytes(data);
                Type.Text = "バイナリデータの16進数文字列です";
                Data.Text = data;
            }
            else if (type == RequestType.SignTransaction)
            {
                parsedTransaction = SymbolTransaction.ParseTransaction(data);
                parsedTransaction.transaction.SignerPublicKey = new PublicKey(Converter.HexToBytes(mainAccount.publicKey));
                Type.Text = "Symbolのトランザクションです";
                Data.Text = parsedTransaction.parsedTransaction;
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
            if (type == RequestType.SignTransaction)
            {
                var network = parsedTransaction.transaction.Network == CatSdk.Symbol.NetworkType.MAINNET ? CatSdk.Symbol.Network.MainNet : CatSdk.Symbol.Network.TestNet;
                var facade = new CatSdk.Facade.SymbolFacade(network);
                var signature = facade.SignTransaction(keyPair, parsedTransaction.transaction);
                var signedTransaction = CatSdk.Symbol.Factory.TransactionsFactory.AttachSignatureTransaction(parsedTransaction.transaction, signature);
                var signedPayload = Converter.BytesToHex(signedTransaction.Serialize());
                var url = $"{callbackUrl}?signed_payload={signedPayload}&pubkey={mainAccount.publicKey}&original_data={data}";
                await Launcher.OpenAsync(new Uri(url));
            }
            else
            {
                var signature = keyPair.Sign(bytesData);
                var url = $"{callbackUrl}?signature={Converter.BytesToHex(signature.bytes)}&pubkey={mainAccount.publicKey}&original_data={data}";
                await Launcher.OpenAsync(new Uri(url));
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
        bytesData = null;
        mainAccount = null;
    }
}