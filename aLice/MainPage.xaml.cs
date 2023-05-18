using System.Text.Json;
using ZXing.Net.Maui;

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
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
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
                
                var keyButton = new Button()
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                };
                if (acc.isMain)
                {
                    keyButton.ImageSource = "star_solid.png";
                    keyButton.Clicked += async (sender, e) =>
                    {
                        await DisplayAlert("Info", "メインアカウントです", "OK");
                    };
                    Grid.SetColumn(keyButton, 1);
                    grid.Children.Add(keyButton);
                }
                else
                {
                    keyButton.ImageSource = "star_regular.png";
                    keyButton.Clicked += async (sender, e) =>
                    {
                        await SetMainAccount(acc.address);
                        await DisplayAlert("Success", "メインアカウントが変更されました", "OK");
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
                    Grid.SetColumn(keyButton, 1);
                    Grid.SetColumn(deleteButton, 2);
                    grid.Children.Add(keyButton);
                    grid.Children.Add(deleteButton);
                }
                
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
            var elementsToRemove = savedAccounts.accounts.Where(acc => acc.address == addressToRemove).ToList();
            savedAccounts.accounts.RemoveAll(x => elementsToRemove.Contains(x));
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
        // データ登録用のボップアップを開く
        await Navigation.PushModalAsync(new InputAccount(this));
    }
}