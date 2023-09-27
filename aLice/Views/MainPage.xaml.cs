using System.ComponentModel;
using aLice.Models;
using aLice.ViewModels;

namespace aLice.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
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
            await DisplayAlert("Error", e.Message, "閉じる");
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
                addressButton.Clicked += async (sender, e) =>
                {
                    await OnAddressCopyButtonClicked(acc.address);
                };
                
                var exportButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    ImageSource = "key.png",
                };
                exportButton.Clicked += async (sender, e) =>
                {
                    await OnExportButtonClicked(acc.address);
                };
                
                var deleteButton = new Button
                {
                    BackgroundColor = Color.FromRgba(255, 255, 255, 0),
                    HorizontalOptions = LayoutOptions.End,
                    ImageSource = "trash_can_regular.png",
                };
                deleteButton.Clicked += async (sender, e) =>
                {
                    await OnDeleteButtonClicked(acc.address, acc.accountName);
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
                        await OnChangeMainAccount(acc.address, acc.accountName);
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
    
    private async Task OnChangeMainAccount(string address, string name)
    {
        try
        {
            await AccountViewModel.ChangeMainAccount(address);
            await DisplayAlert("Success", $"Mainアカウントを{name}に変更しました", "閉じる");
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "閉じる");
        }
    }

    private async Task OnAddressCopyButtonClicked(string address)
    {
        try
        {
            await Clipboard.SetTextAsync(address);
            await DisplayAlert("Copied", "クリップボードにアドレスをコピーしました", "閉じる");
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "閉じる");
        }
    }

    private async Task OnExportButtonClicked(string address)
    {
        try
        {
            var password = await DisplayPromptAsync("Password", "パスワードを入力してください", "Sign", "Cancel", "Input Password", -1, Keyboard.Numeric);
            if (password == null) return;

            var privateKey = AccountViewModel.ExportAccount(address, password);
            await Clipboard.SetTextAsync(privateKey);
            await DisplayAlert("クリップボードに秘密鍵をコピーしました", "秘密鍵の取り扱いには十分に注意してください", "はい");    
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "閉じる");
        }
    }
    
    private async Task OnDeleteButtonClicked(string address, string name)
    {
        try
        {
            await DisplayAlert("Alert", $"本当に{name}を削除していいですか？", "はい", "いいえ");
            await AccountViewModel.DeleteAccount(address);
            await DisplayAlert("Deleted", "アカウントを削除しました", "閉じる");
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "閉じる");
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
}