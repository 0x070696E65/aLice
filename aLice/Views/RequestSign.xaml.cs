using aLice.Models;
using aLice.Resources;
using aLice.ViewModels;

namespace aLice.Views;

public partial class RequestSign : ContentPage
{
    public RequestSign()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            AccountViewModel.DeletePasswordByTimestamp();
            await ShowRequestSign();

            if (RequestViewModel.Notification.SetPublicKey != null 
                && AccountViewModel.MainAccount.publicKey != RequestViewModel.Notification.SetPublicKey
                && RequestViewModel.Notification.RequestType != RequestType.SignCosignature)
            {
                var requestAccount = RequestViewModel.GetRequestAccount();
                var isChangeMainAccount = await DisplayAlert("Confirm",
                    $"{string.Format(AppResources.RequestSign_ConfirmChangeAccount, requestAccount.accountName)}\n{AppResources.RequestSign_ConfirmChangeAccountDescription}", AppResources.LangUtil_Yes, AppResources.LangUtil_No);
                if (isChangeMainAccount)
                {
                    await AccountViewModel.ChangeMainAccount(requestAccount.address);
                    if (RequestViewModel.Notification.RequestType != RequestType.SignUtf8
                        && RequestViewModel.Notification.RequestType != RequestType.SignBinaryHex)
                    {
                        RequestViewModel.SetMainAccountSignerPublicKey();
                    }
                    Password.Text = "";
                    Ask.Text = string.Format(AppResources.RequestSign_ConfirmSign, AccountViewModel.MainAccount.accountName);
                }
                else
                {
                    if (Navigation.ModalStack.Count > 0)
                    {
                        await Navigation.PopModalAsync();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);
            if (Navigation.ModalStack.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
        }
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
            Password.Text = "";
            Password.IsVisible = !AccountViewModel.MainAccount.isBiometrics;

            try
            {
                var p = (await SecureStorage.GetAsync("CurrentPassword")).Split("_");
                Password.Text = p[0];
            }
            catch
            {
                // 何もしない
            }
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestSign(object sender, EventArgs e)
    {
        try
        {
            var (resultType, result) = await RequestViewModel.Accept(Password.Text);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        
            switch (resultType)
            {
                case ResultType.Callback:
                    await Launcher.OpenAsync(new Uri(result));
                    break;
                case ResultType.Announce:
                    await Application.Current.MainPage.Navigation.PushModalAsync(new WaitConfirmed(result, AnnounceType.Normal));
                    break;
                case ResultType.AnnounceBonded:
                    await Application.Current.MainPage.Navigation.PushModalAsync(new WaitConfirmed(result, AnnounceType.Bonded));
                    break;
                case ResultType.AnnounceCosignature:
                    await Application.Current.MainPage.Navigation.PushModalAsync(new WaitConfirmed(result, AnnounceType.Cosignature));
                    break;
                case ResultType.ShowData:
                    await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("Signature", result));
                    break;
                default:
                    throw new Exception("指定されたタイプは存在しません");
            }

            await AccountViewModel.DeletePassword();
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
    
    private async void OnChangeAccount(object sender, EventArgs e)
    {
        try
        {
            var accName = await DisplayActionSheet(AppResources.RequestGetPubKey_AccountSwitching, "cancel", null, AccountViewModel.AccountNames);
            if (accName is null or "cancel")
            {
                return;
            }
            var address = AccountViewModel.Accounts.accounts.ToList().Find(a=>a.accountName == accName).address;
            await AccountViewModel.ChangeMainAccount(address);
            RequestViewModel.SetMainAccountSignerPublicKey();
            Password.Text = "";
            Ask.Text = string.Format(AppResources.RequestSign_ConfirmSign, AccountViewModel.MainAccount.accountName);   
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
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
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
}