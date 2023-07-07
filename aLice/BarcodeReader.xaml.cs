using System.Text.Json;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;
using ZXing.Net.Maui;

namespace aLice;

public partial class BarcodeReader : ContentPage
{
    public event EventHandler<DataEventArgs> DataChanged;
    
    public BarcodeReader()
    {
        InitializeComponent();
    }
    
    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        foreach (var barcode in e.Results)
        {
            var (isCorrectFormat, networkId, privateKey) = ParseQrForPrivateKey(barcode.Value);
            if (!isCorrectFormat) continue;
            var network = networkId switch
            {
                152 => CatSdk.Symbol.Network.TestNet,
                104 => CatSdk.Symbol.Network.MainNet,
                _ => throw new Exception("Invalid network id")
            };
            var facade = new SymbolFacade(network);
            var keyPair = new KeyPair(new PrivateKey(privateKey)); 
            var address = facade.Network.PublicKeyToAddress(keyPair.PublicKey);
            
            DataChanged?.Invoke(this, new DataEventArgs(privateKey, address.ToString(), networkId));
            return;
        }
    }
    
    private async void OnQRCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
    
    private (bool isCorrectFormat, int networkId, string privateKey) ParseQrForPrivateKey(string value)
    {
        try
        {
            var qrFormat = JsonSerializer.Deserialize<QrFormat>(value);
            return qrFormat.type != 2 ? (false, 0, null) : (true, qrFormat.network_id, qrFormat.data.privateKey);
        }
        catch
        {
            return (false, 0, null);
        }
    }
    
    void OnContentPageUnloaded(object sender, EventArgs e)
    {
        cameraBarcodeReaderView.Handler?.DisconnectHandler();
    }
    
    private class QrFormat
    {
        public int v { get; set; }
        public int type { get; set; }
        public int network_id { get; set; }
        public string chain_id { get; set; }
        public QrData data { get; set; }
    }

    private class QrData
    {
        public string privateKey { get; set; }
    }
}

public class DataEventArgs : EventArgs
{
    public string privateKey { get; set; }
    public string address { get; set; }
    public int network { get; set; }

    public DataEventArgs(string _privateKey, string _address, int _network)
    {
        privateKey = _privateKey;
        address = _address;
        network = _network;
    }
}