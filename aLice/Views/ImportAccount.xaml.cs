using aLice.ViewModels;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice.Views;

public partial class ImportAccount : ContentPage
{
    private string NetworkType = "MainNet";
    public ImportAccount()
    {
        InitializeComponent();
        ShowPasswordButton.Text = "\uf06e";
    }

    // 登録ボタンが押されたときに呼び出される
    private async void OnClickSubmitAccount(object sender, EventArgs e)
    {
        try
        {
            var keyPair = new KeyPair(new PrivateKey(PrivateKey.Text));
            
            // バリデーション
            var validate = AccountViewModel.ValidationAccount(
                Name.Text, 
                Address.Text, 
                PrivateKey.Text != null,
                keyPair.PublicKey.bytes,
                Password.Text,
                NetworkType
                );
            if (!validate.isValid)
                throw new Exception(validate.message);
            
            var showText = $"AccountName: {Name.Text}\nAddress: {Address.Text}\n";

            // ダイアログを表示
            var result = 
                Application.Current != null 
                && Application.Current.MainPage != null 
                && Application.Current != null 
                && await Application.Current.MainPage.DisplayAlert("こちらを登録します", showText, "はい", "いいえ");
            
            if (!result) return;
            
            // 秘密鍵を暗号化
            var encryptedPrivateKey = CatSdk.Crypto.Crypto.EncryptString(PrivateKey.Text, Password.Text, Address.Text);

            // 保存処理
            await AccountViewModel.SaveAccount(
                Name.Text,
                Address.Text,
                Converter.BytesToHex(keyPair.PublicKey.bytes),
                encryptedPrivateKey,
                NetworkType
            );
            
            // 保存完了のメッセージを表示
            await DisplayAlert("Saved", "アカウントが登録されました", "OK");
            
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
        await Navigation.PopModalAsync();
    }
    
    private void ShowPasswordButtonClicked(object sender, EventArgs e)
    {
        Password.IsPassword = !Password.IsPassword;
        ShowPasswordButton.Text = Password.IsPassword ? "\uf06e" : "\uf070";
    }
    
    private async void OnQRButtonClicked(object sender, EventArgs e)
    {
        var barcodeReaderPage = new BarcodeReader();
        barcodeReaderPage.DataChanged += OnBarcodeReaderDataChanged;
        await Navigation.PushModalAsync(barcodeReaderPage);
    }
    
    private async void OnBarcodeReaderDataChanged(object sender, DataEventArgs e)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            PrivateKey.Text = e.privateKey;
            Address.Text = e.address;
            switch (e.network)
            {
                case 104:
                    mainnetRadioButton.IsChecked = true;
                    testnetRadioButton.IsChecked = false;
                    NetworkType = "MainNet";
                    break;
                case 152:
                    mainnetRadioButton.IsChecked = false;
                    testnetRadioButton.IsChecked = true;
                    NetworkType = "TestNet";
                    break;
            }

            if (Navigation.ModalStack.Count > 1)
            {
                await Navigation.PopModalAsync();
            }
        });
    }
}