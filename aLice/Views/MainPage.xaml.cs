using System.ComponentModel;
using aLice.Models;
using aLice.Resources;
using aLice.ViewModels;
using System.Globalization;

namespace aLice.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }
    
    private void OnLocalizeChanged(object sender, EventArgs e)
    {
        // 現在のカルチャを取得
        var currentCulture = CultureInfo.CurrentCulture.Name;

        // 現在のカルチャが英語であれば日本語に、日本語であれば英語に切り替える
        if (currentCulture == "en-US")
        {
            CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
            CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
        }
        else
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }
    }
    
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            await AccountViewModel.SetAccounts();
            ShowAccounts();

            // isMainが変更された際のイベントを各アカウントに登録
            foreach (var accountsAccount in AccountViewModel.Accounts.accounts)
            {
                accountsAccount.PropertyChanged += PropertyChangedHandler;
            }
        
            // アカウント群に追加や削除があった際に…
            AccountViewModel.Accounts.accounts.CollectionChanged += (sender, e) =>
            {
                // ShowAccountsを呼び出す
                ShowAccounts();
            
                // 追加されたアカウントにイベントを登録
                if (e.NewItems?[0] != null)
                {
                    ((SavedAccount) e.NewItems[0]).PropertyChanged += PropertyChangedHandler;                    
                }
            };   
        }
        catch(Exception e)
        {
            await DisplayAlert("Error", e.Message, AppResources.LangUtil_Close);
        }
    }
    
    // 保存されているアカウントを表示するための関数
    private void ShowAccounts()
    {
        AccountList.Children.Clear();
        try
        {
            // 保存されているアカウントを取得
            var savedAccounts = AccountViewModel.Accounts;

            foreach (var acc in savedAccounts.accounts)
            {
                var stackLayout = new StackLayout()
                {
                    BackgroundColor= Color.Parse("#F5F5F5"),
                    Padding = 10,
                };
                
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    }
                };
                var nameLabel = new Label
                {
                    Text = acc.accountName,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(nameLabel, 0);
                grid.Children.Add(nameLabel);
                
                var pullDownButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,
                    Text = "▽",
                    TextColor = Colors.Black,
                    FontSize = 12,
                };

                pullDownButton.Clicked += async (sender, args) =>
                {
                    string action;
                    if (acc.isMain)
                    {
                        action = await DisplayActionSheet("Select", "cancel", null, AppResources.LangUtil_AddressCopy, AppResources.LangUtil_PublicKeyCopy, AppResources.LangUtil_PrivateKeyCopy, AppResources.LangUtil_Biometrics, AppResources.LangUtil_Delete);
                    }
                    else
                    {
                        action = await DisplayActionSheet("Select", "cancel", null, AppResources.MainPage_SettingMainAccount,AppResources.LangUtil_AddressCopy, AppResources.LangUtil_PublicKeyCopy, AppResources.LangUtil_PrivateKeyCopy, AppResources.LangUtil_Biometrics, AppResources.LangUtil_Delete);
                    }

                    if (action == AppResources.MainPage_SettingMainAccount)
                    {
                        await OnChangeMainAccount(acc.address, acc.accountName);
                    } else if (action == AppResources.LangUtil_AddressCopy)
                    {
                        await OnAddressCopyButtonClicked(acc.address);
                    } else if (action == AppResources.LangUtil_PublicKeyCopy)
                    {
                        await OnPublicKeyCopyButtonClicked(acc.publicKey);
                    } else if (action == AppResources.LangUtil_PrivateKeyCopy)
                    {
                        await OnExportButtonClicked(acc.address);
                    } else if (action == AppResources.LangUtil_Delete)
                    {
                        await OnDeleteButtonClicked(acc.address, acc.accountName);
                    } else if (action == AppResources.LangUtil_Biometrics)
                    {
                        await OnUseBionicButtonClicked(acc.address);
                    }
                };
                
                Grid.SetColumn(pullDownButton, 1);
                grid.Children.Add(pullDownButton);
                                
                var addressLabel = new Label
                {
                    Text = acc.address,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                var networkLabel = new Label
                {
                    Text = acc.networkType,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End,
                    FontSize = 10,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                
                stackLayout.Children.Add(grid);
                stackLayout.Children.Add(addressLabel);
                stackLayout.Children.Add(networkLabel);
                if (acc.isMain) {
                    AccountList.Children.Insert(0, stackLayout);    
                }
                else {
                    AccountList.Children.Add(stackLayout);   
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("No accounts found: " + e.Message);
        }
    }
    
    private async void OnButtonClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("Select", "cancel", null, "New Account", "Import Account");
        switch (action)
        {
            case "New Account":
                await Navigation.PushModalAsync(new NewAccount());
                break;
            case "Import Account":
                await Navigation.PushModalAsync(new ImportAccount());
                break;
        }
    }

    private async void OnOpenSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new Settings());
    }
    
    private async Task OnChangeMainAccount(string address, string name)
    {
        try
        {
            await AccountViewModel.ChangeMainAccount(address);
            await DisplayAlert("Success", string.Format(AppResources.MainPage_ChangedAccount, name), AppResources.LangUtil_Close);
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }

    private async Task OnAddressCopyButtonClicked(string address)
    {
        try
        {
            await Clipboard.SetTextAsync(address);
            await DisplayAlert("Copied", AppResources.MainPage_CopiedAddressToClipBoard, AppResources.LangUtil_Close);
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }
    
    private async Task OnPublicKeyCopyButtonClicked(string publicKey)
    {
        try
        {
            await Clipboard.SetTextAsync(publicKey);
            await DisplayAlert("Copied", AppResources.MainPage_CopiedPublicKeyToClipBoard, AppResources.LangUtil_Close);
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }

    private async Task OnExportButtonClicked(string address)
    {
        try
        {
            var password = await DisplayPromptAsync("Password", AppResources.LangUtil_InputPassword, "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
            if (password == null) return;

            var privateKey = AccountViewModel.ExportAccount(address, password);
            await Clipboard.SetTextAsync(privateKey);
            await DisplayAlert(AppResources.MainPage_CopiedPrivateKeyToClipBoard, AppResources.MainPage_WarningPrivateKey, AppResources.LangUtil_Yes);    
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }
    
    private async Task OnDeleteButtonClicked(string address, string name)
    {
        try
        {
            var result = await DisplayAlert("Alert", string.Format(AppResources.MainPage_ConfirmDeleteAccount, name), AppResources.LangUtil_Yes, AppResources.LangUtil_No);
            if (result)
            {
                await AccountViewModel.DeleteAccount(address);
                await DisplayAlert("Deleted", AppResources.MainPage_DeletedAccount, AppResources.LangUtil_Close);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }
    
    private async Task OnUseBionicButtonClicked(string address)
    {
        try
        {
            var isUseBionic = AccountViewModel.IsUseBionic(address);
            Console.WriteLine(isUseBionic);
            var message = isUseBionic ? AppResources.MainPage_ConfirmDeactivateBionic : AppResources.MainPage_ConfirmActivateBionic;
            var resultMessage = isUseBionic ? AppResources.MainPage_DeactivatedBionic : AppResources.MainPage_ActivatedBionic;
            var password = await DisplayPromptAsync("Password", $"{message}\n{AppResources.LangUtil_InputPassword}", "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
            if (password == null) return;
            var privateKey = AccountViewModel.ExportAccount(address, password);
            await AccountViewModel.UpdateUseBionic(address, privateKey, isUseBionic);
            await DisplayAlert("Success", resultMessage, AppResources.LangUtil_Close);
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, AppResources.LangUtil_Close);
        }
    }
    
    // 変更されたプロパティがisMainであればShowAccountsを呼び出す（メインアカウントの変更を反映させるため）
    private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "isMain")
        {
            ShowAccounts();
        }
    }
    
    private async void OnQRButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new QrReaderForSign());
    }
}