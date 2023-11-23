using System.Net.WebSockets;
using aLice.Models;
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
    public WaitConfirmed(string payload)
    {
        InitializeComponent();
        Payload = payload;
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
            var (hash, address) = Symbol.GetHash(Payload);
            Hash = hash;
            Data.Text = RequestViewModel.ParseTransaction[0].parsedTransaction;
            var node = RequestViewModel.Notification.Node;
            using var w = new ClientWebSocket();
            var websocket = new ListenerService(node, w);
            using var w2 = new ClientWebSocket();
            var websocket2 = new ListenerService(node, w2);
            
            await websocket.Open();
            Console.WriteLine(address);
            var confirmedTask = websocket.Confirmed(address, token, async (tx) =>
            {
                Console.WriteLine("CONFIRMED");
                if (token.IsCancellationRequested)
                {
                    websocket.Close();
                    return;
                }
                if ((string)tx["meta"]?["hash"] == Hash)
                {
                    IsConfirmed = true;
                    Indicator.IsRunning = false;
                    Result.Text = "トランザクションが承認されました";
                    websocket.Close();
                };
            });
            
            await websocket2.Open();
            var statusTask = websocket2.Status(address, token, (n) =>
            {
                Console.WriteLine("STATUS");
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
            var announceTask = symbol.Announce(Payload);

            await Task.WhenAll(confirmedTask, statusTask, announceTask);
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            await Console.Error.WriteLineAsync(exception.StackTrace);
            await DisplayAlert("Error", exception.Message, "閉じる");
        }
    }
    
    private async void OnToExplorer(object sender, EventArgs e)
    {
        var explorer = RequestViewModel.ParseTransaction[0].transaction.Network == CatSdk.Symbol.NetworkType.MAINNET ? "https://symbol.fyi/transactions/" : "https://testnet.symbol.fyi/transactions/";
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