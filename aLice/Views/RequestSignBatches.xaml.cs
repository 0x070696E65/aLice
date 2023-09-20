using aLice.ViewModels;

namespace aLice.Views;

public partial class RequestSignBatches : ContentPage
{
    public RequestSignBatches()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (RequestViewModel.Notification.SetPublicKey != null && AccountViewModel.MainAccount.publicKey != RequestViewModel.Notification.SetPublicKey)
        {
            var requestAccount = RequestViewModel.GetRequestAccount();
            var isChangeMainAccount = await DisplayAlert("確認", requestAccount.accountName + "をメインアカウントに変更しますか？\nいいえを選択するとこのページを閉じます", "はい", "いいえ");
            if (isChangeMainAccount)
            {
                AccountViewModel.ChangeMainAccount(requestAccount.accountName);
            }
            else
            {
                await Navigation.PopModalAsync();
            }
        }
        await ShowRequestSign();
    }

    // 署名を要求されたときに呼び出される
    private async Task ShowRequestSign()
    {
        try
        {
            var (domainText, typeText, dataText, askText) = RequestViewModel.GetShowTexts();
            Domain.Text = domainText;
            Type.Text = typeText;
            Data.Text = dataText;
            Ask.Text = askText;
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestSign(object sender, EventArgs e)
    {
        try
        {
            var password = await DisplayPromptAsync("Password", "パスワードを入力してください", "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
            var (isCallBack, result) = await RequestViewModel.Accept(password);
            
            await Application.Current.MainPage.Navigation.PopModalAsync();
            
            if (isCallBack)
            {
                await Launcher.OpenAsync(new Uri(result));
            }
            else
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("署名データ", result));
            }
        } 
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    private async void OnChangeAccount(object sender, EventArgs e)
    {
        try
        {
            var accName = await DisplayActionSheet("アカウント切り替え", "cancel", null, AccountViewModel.AccountNames);
            AccountViewModel.ChangeMainAccount(accName);
            Ask.Text = $"{AccountViewModel.MainAccount.accountName}の公開鍵を渡しても良いですか？";   
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    // 署名を拒否したときに呼び出される
    private async void RejectedRequestSign(object sender, EventArgs e)
    {
        try
        {
            var (isCallBack, result) = RequestViewModel.Reject();
            if (isCallBack)
            {
                await Launcher.OpenAsync(new Uri(result));
            }
            await Navigation.PopModalAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
}