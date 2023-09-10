using System.Text;
using System.Text.Json;
using CatSdk.Crypto;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class RequestSign : ContentPage
{
    private string data;
    private string callbackUrl;
    private readonly string method;
    private readonly string redirectUrl;
    private readonly string setPublicKey;
    private readonly string recipientPublicKeyForEncryptMessage;
    private readonly string feeMultiplier;
    private SavedAccounts savedAccounts;
    private readonly RequestType type;
    private byte[] bytesData;
    private SavedAccount mainAccount;
    private (ITransaction transaction, string parsedTransaction) parsedTransaction;

    public RequestSign(string _data, string _callbackUrl, RequestType _type, string _method, string _redirectUrl = null, string _setPublicKey = null, string _recipientPublicKeyForEncryptMessage = null, string _feeMultiplier = null)
    {
        InitializeComponent();
        data = _data;
        method = _method;
        redirectUrl = _redirectUrl;
        callbackUrl = _callbackUrl;
        setPublicKey = _setPublicKey;
        type = _type;
        recipientPublicKeyForEncryptMessage = _recipientPublicKeyForEncryptMessage;
        feeMultiplier = _feeMultiplier;
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
            if (callbackUrl != null)
            {
                var uri = new Uri(callbackUrl);
                var baseUrl = $"{uri.Scheme}://{uri.Authority}";
                Domain.Text = $"{baseUrl}からの署名要求です";    
            }
            
            var accounts = await SecureStorage.GetAsync("accounts");
            savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            if (savedAccounts.accounts[0] == null) throw new NullReferenceException("アカウントが登録されていません");
            mainAccount = savedAccounts.accounts.Find((acc) => acc.isMain);

            if (setPublicKey != null && mainAccount.publicKey != setPublicKey)
            {
                await SetMainAccount(setPublicKey);
            }
            
            if(type == RequestType.SignUtf8)
            {
                bytesData = Converter.HexToBytes(data);
                Type.Text = "UTF8文字列です";
                Data.Text = Converter.HexToUtf8(data);
            } else if(type == RequestType.SignBinaryHex)
            {
                bytesData = Converter.HexToBytes(data);
                Type.Text = "バイナリデータの16進数文字列です";
                Data.Text = data;
            }
            else if (type == RequestType.SignTransaction)
            {
                parsedTransaction = SymbolTransaction.ParseTransaction(data, recipientPublicKeyForEncryptMessage, feeMultiplier);
                parsedTransaction.transaction.SignerPublicKey = new PublicKey(Converter.HexToBytes(mainAccount.publicKey));
                Type.Text = "Symbolのトランザクションです";
                Data.Text = parsedTransaction.parsedTransaction;

                if (recipientPublicKeyForEncryptMessage != null && parsedTransaction.transaction.Type == TransactionType.TRANSFER)
                {
                    Type.Text += "\nメッセージの暗号化を行います。";
                }
            }
            Ask.Text = $"{mainAccount.accountName}で署名しますか？";
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
        Ask.Text = $"{mainAccount.accountName}で署名しますか？";
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
                if (recipientPublicKeyForEncryptMessage != null && parsedTransaction.transaction.Type == TransactionType.TRANSFER)
                {
                    var transferTransaction = parsedTransaction.transaction as TransferTransactionV1;
                    var bytes = transferTransaction?.Message;
                    if (bytes != null && bytes[0] == 1 && bytes.Length >= 2)
                    {
                        var message = Encoding.UTF8.GetString(bytes.ToList().GetRange(1, bytes.Length - 1).ToArray());
                        var encrypted = "01" + Crypto.Encode(privateKey, recipientPublicKeyForEncryptMessage, message);
                        transferTransaction.Message = Converter.HexToBytes(encrypted);
                        TransactionHelper.SetMaxFee(transferTransaction,
                            feeMultiplier != null ? int.Parse(feeMultiplier) : 100);
                    }
                }
                var network = parsedTransaction.transaction.Network == CatSdk.Symbol.NetworkType.MAINNET ? CatSdk.Symbol.Network.MainNet : CatSdk.Symbol.Network.TestNet;
                var facade = new CatSdk.Facade.SymbolFacade(network);
                var signature = facade.SignTransaction(keyPair, parsedTransaction.transaction);
                var signedTransaction = CatSdk.Symbol.Factory.TransactionsFactory.AttachSignatureTransaction(parsedTransaction.transaction, signature);
                var signedPayload = Converter.BytesToHex(signedTransaction.Serialize());
                switch (method)
                {
                    case "post":
                    {
                        var dic = new Dictionary<string, string>
                        {
                            {"pubkey", mainAccount.publicKey},
                            {"original_data", data},
                            {"signed_payload", signedPayload},
                        };
                        using var client = new HttpClient();
                        var content = new StringContent(JsonSerializer.Serialize(dic), Encoding.UTF8, "application/json");
                        var response =  client.PostAsync(callbackUrl, content).Result;
                        await response.Content.ReadAsStringAsync();
                        if (redirectUrl != null) await Launcher.OpenAsync(new Uri(redirectUrl));
                        await Navigation.PopModalAsync();
                        break;
                    }
                    case "get":
                    {
                        if (callbackUrl != null)
                        {
                            var additionalParam =
                                $"signed_payload={signedPayload}&pubkey={mainAccount.publicKey}&original_data={data}";
                            if (callbackUrl.Contains('?')) {
                                callbackUrl += "&" + additionalParam;
                            }
                            else {
                                callbackUrl += "?" + additionalParam;
                            }
                            await Launcher.OpenAsync(new Uri(callbackUrl));
                            await Navigation.PopModalAsync();
                            break;   
                        }
                        else
                        {
                            await Application.Current.MainPage.Navigation.PopModalAsync();
                            await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("署名データ", signedPayload));
                            break;
                        }
                    }
                    default:
                        throw new Exception("不正なリクエストです");
                }
            }
            else
            {
                var signature = keyPair.Sign(bytesData);
                switch (method)
                {
                    case "post":
                    {
                        var dic = new Dictionary<string, string>
                        {
                            {"pubkey", mainAccount.publicKey},
                            {"original_data", data},
                            {"signature", Converter.BytesToHex(signature.bytes)},
                        };
                        using var client = new HttpClient();
                        var content = new StringContent(JsonSerializer.Serialize(dic), Encoding.UTF8, "application/json");
                        var response =  client.PostAsync(callbackUrl, content).Result;
                        await response.Content.ReadAsStringAsync();
                        if (redirectUrl != null) await Launcher.OpenAsync(new Uri(redirectUrl));
                        await Navigation.PopModalAsync();
                        break;
                    }
                    case "get":
                    {
                        if (callbackUrl != null)
                        {
                            var additionalParam =
                                $"signature={Converter.BytesToHex(signature.bytes)}&pubkey={mainAccount.publicKey}&original_data={data}";
                            if (callbackUrl.Contains('?')) {
                                callbackUrl += "&" + additionalParam;
                            }
                            else {
                                callbackUrl += "?" + additionalParam;
                            }
                            await Launcher.OpenAsync(new Uri(callbackUrl));
                            await Navigation.PopModalAsync();
                            break;
                        }
                        else
                        {
                            await Application.Current.MainPage.Navigation.PopModalAsync();
                            await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("署名データ", Converter.BytesToHex(signature.bytes)));
                            break;
                        }
                    }
                }
            }
            Reset();
        } 
        catch (Exception exception)
        {
            Error.Text = exception.Message;
        }
    }
    
    private async Task SetMainAccount(string publicKey)
    {
        var accounts = await SecureStorage.GetAsync("accounts");
        var _savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
        var requestAccount = _savedAccounts.accounts.Find(acc => acc.publicKey == publicKey);
        if (requestAccount == null)
        {
            await DisplayAlert("確認", "指定されたアカウントが存在しません", "閉じる");
            await Navigation.PopModalAsync();
        }
        else
        {
            if (requestAccount.isMain) return;
            var isChangeMainAccount = await DisplayAlert("確認", requestAccount.accountName + "をメインアカウントに変更しますか？\nいいえを選択するとこのページを閉じます", "はい", "いいえ");
            if (isChangeMainAccount)
            {
                _savedAccounts.accounts.ForEach(acc => acc.isMain = acc.publicKey == publicKey);
                var updatedAccounts = JsonSerializer.Serialize(_savedAccounts);
                await SecureStorage.SetAsync("accounts", updatedAccounts);
                mainAccount = requestAccount;
            }
            else
            {
                await Navigation.PopModalAsync();
            }
        }
    }
    
    // 署名を拒否したときに呼び出される
    private async void RejectedRequestSign(object sender, EventArgs e)
    {
        if (callbackUrl != null)
        {
            const string additionalParam = "error=sign_rejected";
            if (callbackUrl.Contains('?'))
            {
                callbackUrl += "&" + additionalParam;
            }
            else
            {
                callbackUrl += "?" + additionalParam;
            }

            await Launcher.OpenAsync(new Uri(callbackUrl));
        }
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