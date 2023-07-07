using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public partial class NewAccount : ContentPage
{
    private string NetworkType = "MainNet";
    private readonly MainPage mainPage;
    private SymbolFacade facade;
    private string address;
    public NewAccount(MainPage _mainPage)
    {
        InitializeComponent();
        ShowPasswordButton.Text = "\uf06e";
        mainPage = _mainPage;
    }

    // 登録ボタンが押されたときに呼び出される
    private async void OnClickSubmitAccount(object sender, EventArgs e)
    {
        try
        {
            // バリデーション
            var validate = await ValidationAccount();
            if (!validate.isValid)
                throw new Exception(validate.message);

            var keyPair = KeyPair.GenerateNewKeyPair();
            
            var networkType = NetworkType switch
            {
                "MainNet" => CatSdk.Symbol.Network.MainNet,
                "TestNet" => CatSdk.Symbol.Network.TestNet,
                _ => throw new Exception("NetworkTypeが正しくありません")
            };
            facade = new SymbolFacade(networkType);
            // 秘密鍵を暗号化
            address = facade.Network.PublicKeyToAddress(keyPair.PublicKey.bytes).ToString();
            var encryptedPrivateKey = CatSdk.Crypto.Crypto.EncryptString(keyPair.PrivateKey.ToString(), Password.Text, address);
            var showText = $"AccountName: {Name.Text}\nAddress: {address}\n";

            // ダイアログを表示
            var result = 
                Application.Current != null 
                && Application.Current.MainPage != null 
                && Application.Current != null 
                && await Application.Current.MainPage.DisplayAlert("こちらを登録します", showText, "はい", "いいえ");
            
            if (!result) return;

            var saveAccount = new SavedAccount()
            {
                isMain = false,
                accountName = Name.Text,
                address = address,
                publicKey = Converter.BytesToHex(keyPair.PublicKey.bytes),
                encryptedPrivateKey = encryptedPrivateKey,
                networkType = NetworkType
            };
            
            // 保存処理
            await SaveAccount(saveAccount);
        }
        catch (Exception error)
        {
            await Console.Error.WriteLineAsync(error.Message);
            Error.Text = error.Message;
        }
    }
    
    // アカウントを保存する
    private async Task SaveAccount(SavedAccount savedAccount)
    {
        SavedAccounts savedAccounts;
        try
        {
            // 保存されているアドレスを取得
            var accounts = await SecureStorage.GetAsync("accounts");
            savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
        }
        catch
        {
            savedAccount.isMain = true;
            savedAccounts = new SavedAccounts();
            savedAccounts.accounts = new List<SavedAccount>();
        }
        if (savedAccounts.accounts.Count == 0)
            savedAccount.isMain = true;

        // 保存されているアドレスに追加
        savedAccounts.accounts.Add(savedAccount);
        
        // 保存
        await SecureStorage.SetAsync("accounts", JsonSerializer.Serialize(savedAccounts));
        
        // 保存完了のメッセージを表示
        await DisplayAlert("Saved", "アカウントが登録されました", "OK");

        await mainPage.ShowAccounts();

        // 画面を閉じる
        await Navigation.PopModalAsync();
    }
    
    // ラジオボタンの選択状態が変更されたときに呼び出される
    private void OnRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        
        // 選択されたラジオボタンを取得
        var selectedRadioButton = (RadioButton)sender;
        if (selectedRadioButton == mainnetRadioButton) {
            NetworkType = "MainNet";
        }
        else if (selectedRadioButton == testnetRadioButton) {
            NetworkType = "TestNet";
        }
    }
    
    // アカウントのバリデーション
    private async Task<(bool isValid, string message)> ValidationAccount()
    {
        var message = "";
        var isValid = true;
        
        if (Name.Text == null)
        {
            message += "アカウント名は必須です\n";
            isValid = false;
        }
        if (Password.Text == null)
        {
            message += "パスワードは必須です\n";
            isValid = false;
        }
        try
        {
            // 保存されているアドレスを取得
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            foreach (var savedAccount in savedAccounts.accounts)
            {
                if (savedAccount.accountName == Name.Text)
                {
                    message += "アカウント名はすでに登録されています\n";
                    isValid = false;
                }
                if (savedAccount.address == address)
                {
                    message += "アドレスはすでに登録されています\n";
                    isValid = false;
                }
            }
        }
        catch
        {
            // 保存されているアドレスがない場合は何もしない
        }

        return (isValid, message);
    }
    
    private async void OnClickCloseAccount(object sender, EventArgs e)
    {
        // 画面を閉じる
        await Navigation.PopModalAsync();
    }
    
    private void ShowPasswordButtonClicked(object sender, EventArgs e)
    {
        // パスワードを表示する
        Password.IsPassword = !Password.IsPassword;
        ShowPasswordButton.Text = Password.IsPassword ? "\uf06e" : "\uf070";
    }
}