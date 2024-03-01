using aLice.Resources;
using aLice.ViewModels;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice.Views;

public partial class NewAccount : ContentPage
{
    private string NetworkType = "MainNet";
    public NewAccount()
    {
        InitializeComponent();
        ShowPasswordButton.Text = "\uf06e";
    }

    // 登録ボタンが押されたときに呼び出される
    private async void OnClickSubmitAccount(object sender, EventArgs e)
    {
        try
        {
            var keyPair = KeyPair.GenerateNewKeyPair();
            
            // バリデーション
            var validate = AccountViewModel.ValidationAccount(
                Name.Text, 
                null, 
                false, 
                null, 
                Password.Text, 
                null, 
                true);
            if (!validate.isValid)
                throw new Exception(validate.message);
            
            var networkType = NetworkType switch
            {
                "MainNet" => CatSdk.Symbol.Network.MainNet,
                "TestNet" => CatSdk.Symbol.Network.TestNet,
                _ => throw new Exception(AppResources.LangUtil_IncorrectNetwork)
            };
            var facade = new SymbolFacade(networkType);
            // 秘密鍵を暗号化
            var address = facade.Network.PublicKeyToAddress(keyPair.PublicKey.bytes).ToString();
            var showText = $"AccountName: {Name.Text}\nAddress: {address}\n";

            // ダイアログを表示
            var result = 
                Application.Current != null 
                && Application.Current.MainPage != null 
                && Application.Current != null 
                && await Application.Current.MainPage.DisplayAlert(AppResources.Account_OnClickSubmitAccount_DialigTitle, showText, AppResources.LangUtil_Yes, AppResources.LangUtil_No);
            
            if (!result) return;

            var encryptedPrivateKey = CatSdk.Crypto.Crypto.EncryptString(keyPair.PrivateKey.ToString(), Password.Text, address);

            // 保存処理
            await AccountViewModel.SaveAccount(
                Name.Text,
                address,
                Converter.BytesToHex(keyPair.PublicKey.bytes),
                encryptedPrivateKey,
                NetworkType
            );
            
            // 保存完了のメッセージを表示
            await DisplayAlert("Saved", AppResources.Account_OnClickSubmitAccount_Success, "OK");
            
            // 画面を閉じる
            await Navigation.PopModalAsync();
        }
        catch (Exception error)
        {
            await Console.Error.WriteLineAsync(error.Message);
            Error.Text = error.Message;
        }
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