using aLice.Models;
using aLice.Resources;
using aLice.ViewModels;

namespace aLice.Views;

public partial class RequestGetPubkey : ContentPage
{
    public RequestGetPubkey()
    {
        InitializeComponent();
        Ask.Text = string.Format(AppResources.RequestGetPubKey_Confirm, AccountViewModel.MainAccount.accountName);
    }
    
    private async void OnChangeAccount(object sender, EventArgs e)
    {
        try
        {
            var accName = await DisplayActionSheet(AppResources.RequestGetPubKey_AccountSwitching, "cancel", null, AccountViewModel.AccountNames);
            if (accName is null or "cancel")
                return;
            
            var address = AccountViewModel.Accounts.accounts.ToList().Find(a=>a.accountName == accName).address;
            await AccountViewModel.ChangeMainAccount(address);
            Ask.Text = string.Format(AppResources.RequestGetPubKey_Confirm, AccountViewModel.MainAccount.accountName);   
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestGetPubkey(object sender, EventArgs e)
    {
        try
        {
            var (resultType, result) = await RequestViewModel.Accept();
            if (resultType == ResultType.Callback)
            {
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Launcher.OpenAsync(new Uri(result));
            }
            else
            {
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("PublicKey", result));
            }   
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
    
    // 公開鍵要求を拒否したときに呼び出される
    private async void RejectedRequestGetPubkey(object sender, EventArgs e)
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