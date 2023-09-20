using System.Text.Json;
using aLice.ViewModels;

namespace aLice.Views;

public partial class RequestGetPubkey : ContentPage
{
    public RequestGetPubkey()
    {
        InitializeComponent();
        Ask.Text = $"{AccountViewModel.MainAccount.accountName}の公開鍵を渡しても良いですか？";
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
    
    // 署名を受け入れたときに呼び出される
    private async void AcceptRequestGetPubkey(object sender, EventArgs e)
    {
        try
        {
            var (isCallBack, result) = await RequestViewModel.Accept();
            if (isCallBack)
            {
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Launcher.OpenAsync(new Uri(result));
            }
            else
            {
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Application.Current.MainPage.Navigation.PushModalAsync(new ShowPage("公開鍵", result));
            }   
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "閉じる");
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
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
}