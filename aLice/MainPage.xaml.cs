using System.ComponentModel;
using System.Text.Json;
using ZXing.Net.Maui;
using CatSdk.Symbol;
using CatSdk.CryptoTypes;

namespace aLice;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ShowAccounts();
    }
    
    public async Task ShowAccounts()
    {
        AccountList.Children.Clear();
        try
        {
            // 保存されているアカウントを取得
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);

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
                        new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    }
                };
                var nameLabel = new Label
                {
                    Text = acc.accountName,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(nameLabel, 0);
                grid.Children.Add(nameLabel);
                
                var starButton = new Button()
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                };
                var addressButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    ImageSource = "book.png",
                };
                addressButton.Clicked += async (sender, args) =>
                {
                    await Clipboard.SetTextAsync(acc.address);
                    // ダイアログを表示
                    if(Application.Current != null 
                       && Application.Current.MainPage != null 
                       && Application.Current != null )
                        await Application.Current.MainPage.DisplayAlert("Copied", "クリップボードにアドレスをコピーしました", "閉じる");
                };
                var exportButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    ImageSource = "key.png",
                };
                exportButton.Clicked += async (sender, e) =>
                {
                    await ExportAccount(acc.address);
                };
                var deleteButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    ImageSource = "trash_can_regular.png",
                };
                deleteButton.Clicked += async (sender, e) =>
                {
                    await ConfirmDeleteAccount(acc.address);
                };
                if (acc.isMain)
                {
                    starButton.ImageSource = "star_solid.png";
                    starButton.Clicked += async (sender, e) =>
                    {
                        await DisplayAlert("Info", "メインアカウントです", "OK");
                    };
                }
                else
                {
                    starButton.ImageSource = "star_regular.png";
                    starButton.Clicked += async (sender, e) =>
                    {
                        await SetMainAccount(acc.address);
                        await DisplayAlert("Success", "メインアカウントが変更されました", "OK");
                    };
                }
                
                Grid.SetColumn(starButton, 1);
                Grid.SetColumn(addressButton, 2);
                Grid.SetColumn(exportButton, 3);
                Grid.SetColumn(deleteButton, 4);
                grid.Children.Add(starButton);
                grid.Children.Add(addressButton);
                grid.Children.Add(exportButton);
                grid.Children.Add(deleteButton);
                
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

    private async Task ExportAccount(string address)
    {
        var password = await DisplayPromptAsync("Password", "パスワードを入力してください", "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
        if (password == null) return;
        string privateKey;
        try {
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            if (savedAccounts.accounts[0] == null) throw new NullReferenceException("アカウントが登録されていません");
            var acc = savedAccounts.accounts.Find(acc => acc.address == address);
            privateKey = CatSdk.Crypto.Crypto.DecryptString(acc.encryptedPrivateKey, password, acc.address);
        }
        catch {
            throw new Exception("パスワードが正しくありません");
        }
        var keyPair = new KeyPair(new PrivateKey(privateKey));
        await Clipboard.SetTextAsync(keyPair.PrivateKey.ToString());
        // ダイアログを表示
        if(Application.Current != null 
           && Application.Current.MainPage != null 
           && Application.Current != null )
        await Application.Current.MainPage.DisplayAlert("クリップボードに秘密鍵をコピーしました", "秘密鍵の取り扱いには十分に注意してください", "はい");
    }

    private async Task SetMainAccount(string mainAddress)
    {
        var accounts = await SecureStorage.GetAsync("accounts");
        var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
        savedAccounts.accounts.ForEach(acc => acc.isMain = acc.address == mainAddress);
        var updatedAccounts = JsonSerializer.Serialize(savedAccounts);
        await SecureStorage.SetAsync("accounts", updatedAccounts);
        await ShowAccounts();
    }

    private async Task ConfirmDeleteAccount(string addressToRemove)
    {
        var isOK = await DisplayAlert("Confirm", "本当に削除しますか？", "OK", "Cancel");
        if (!isOK) return;
        await DeleteAccount(addressToRemove);
        await ShowAccounts();
    }

    private async Task DeleteAccount(string addressToRemove)
    {
        try
        {
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            var elementsToRemove = savedAccounts.accounts.Find(acc => acc.address == addressToRemove);
            savedAccounts.accounts.Remove(elementsToRemove);
            if (elementsToRemove.isMain && savedAccounts.accounts.Count > 0)
                savedAccounts.accounts[0].isMain = true;
            
            var updatedAccounts = JsonSerializer.Serialize(savedAccounts);
            await SecureStorage.SetAsync("accounts", updatedAccounts);
        }
        catch (Exception e)
        {
            await DisplayAlert("ERROR", e.Message, "OK");
        }
    }
    
    private async void OnButtonClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("Select", "cancel", null, "New Account", "Import Account");
        switch (action)
        {
            case "New Account":
                await Navigation.PushModalAsync(new NewAccount(this));
                break;
            case "Import Account":
                await Navigation.PushModalAsync(new ImportAccount(this));
                break;
        }
    }
}