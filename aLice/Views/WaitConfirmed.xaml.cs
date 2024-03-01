using System.Net.WebSockets;
using aLice.Models;
using aLice.Resources;
using aLice.Services;
using aLice.ViewModels;
using CatSdk.Symbol;

namespace aLice.Views;

public partial class WaitConfirmed : ContentPage
{
    private readonly string Payload;
    private CancellationTokenSource CancellationTokenSource;
    private string Hash;
    private bool IsConfirmed;
    private AnnounceType AnnounceType;
    public WaitConfirmed(string payload, AnnounceType announceType)
    {
        InitializeComponent();
        Payload = payload;
        AnnounceType = announceType;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Show();
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CancellationTokenSource.Cancel();
    }

    private async Task Show()
    {
        try
        {
            Error.Text = "";
            Result.Text = "";
            Indicator.IsRunning = true;
            CancellationTokenSource = new CancellationTokenSource();
            var token = CancellationTokenSource.Token;

            var hash = "";
            var address = "";

            try
            {
                if (AnnounceType == AnnounceType.Cosignature)
                {
                    var arr = Payload.Split("_");
                    hash = arr[0];
                    address = AccountViewModel.MainAccount.address;
                }
                else
                {
                    (hash, address) = Symbol.GetHash(Payload);
                }
                Data.Text = RequestViewModel.ParseTransaction[0].parsedTransaction;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            
            Hash = hash;
            var node = RequestViewModel.Notification.Node;
            using var w = new ClientWebSocket();
            var websocket = new ListenerService(node, w);
            using var w2 = new ClientWebSocket();
            var websocket2 = new ListenerService(node, w2);
            
            await websocket.Open();
            var confirmedTask = websocket.Confirmed(address, token, async (tx) =>
            {
                if (token.IsCancellationRequested)
                {
                    websocket.Close();
                    return;
                }
                if ((string)tx["meta"]?["hash"] == Hash)
                {
                    IsConfirmed = true;
                    Indicator.IsRunning = false;
                    Result.Text = AppResources.WaitConfirmed_SuccessTransaction;
                    websocket.Close();
                };
            });
            
            await websocket2.Open();
            var statusTask = websocket2.Status(address, token, (n) =>
            {
                if (token.IsCancellationRequested)
                {
                    websocket2.Close();
                    return;
                }
                websocket2.Close();
                Indicator.IsRunning = false;
                Error.Text = "Error:\n" + n["code"];
            });
            
            var symbol = new Symbol(node);
            var announceTask = symbol.Announce(Payload, AnnounceType);
            await Task.WhenAll(confirmedTask, statusTask, announceTask);
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            await Console.Error.WriteLineAsync(exception.StackTrace);
            await DisplayAlert("Error", exception.Message, AppResources.LangUtil_Close);
        }
    }
    
    private async void OnToExplorer(object sender, EventArgs e)
    {
        var explorer = AccountViewModel.MainAccount.networkType == "MainNet" ? "https://symbol.fyi/transactions/" : "https://testnet.symbol.fyi/transactions/";
        await Launcher.OpenAsync(new Uri($"{explorer}{Hash}"));
        if (IsConfirmed) await Close();
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await Close();
    }
    
    private async Task Close()
    {
        CancellationTokenSource.Cancel();
        await Navigation.PopModalAsync();
    }
}