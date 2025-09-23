using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using aLice.Resources;
using SymbolSdk;
using SymbolSdk.Symbol;

namespace aLice.Views;

public partial class BarcodeReader : ContentPage
{
    public event EventHandler<DataEventArgs> DataChanged;
    private HashSet<string> scannedQRCodes = new HashSet<string>();

    public BarcodeReader()
    {
        InitializeComponent();
        cameraView.CamerasLoaded += CameraViewCamerasLoaded;
        cameraView.BarcodeDetected += CameraViewBarcodeDetected;
    }
    
    private void CameraViewCamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.Cameras.Count > 0)
        {
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StopCameraAsync();
                await cameraView.StartCameraAsync();
            });
        }
    }

    private void CameraViewBarcodeDetected(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var qrCodeText = args.Result[0].Text;
            if (scannedQRCodes.Contains(qrCodeText)) return;
            scannedQRCodes.Add(qrCodeText);

            var (isCorrectFormat, networkId, privateKey) = await ParseQrForPrivateKey(args.Result[0].Text);
            scannedQRCodes.Remove(qrCodeText);
            if (!isCorrectFormat) return;
            var network = networkId switch
            {
                152 => SymbolSdk.Symbol.Network.TestNet,
                104 => SymbolSdk.Symbol.Network.MainNet,
                _ => throw new Exception("Invalid network id")
            };
            var facade = new SymbolFacade(network);
            var keyPair = new KeyPair(new PrivateKey(privateKey)); 
            var address = facade.Network.PublicKeyToAddress(keyPair.PublicKey);
            
            DataChanged?.Invoke(this, new DataEventArgs(privateKey, address.ToString(), networkId));
            DataChanged = null;
            if (Navigation.ModalStack.Count > 1)
            {
                await Navigation.PopModalAsync();
            }
        });
    }

    private async Task<(bool isCorrectFormat, int networkId, string privateKey)> ParseQrForPrivateKey(string value)
    {
        try
        {
            var qrFormat = JsonSerializer.Deserialize<QrFormat>(value);
            var privateKey = await GetPrivateKey(qrFormat);
            if (privateKey == null) return (false, 0, null);
            return qrFormat.type != 2 ? (false, 0, null) : (true, qrFormat.network_id, privateKey);
        }
        catch
        {
            return (false, 0, null);
        }
    }

    private async Task<string> GetPrivateKey(QrFormat qrFormat)
    {
        try
        {
            // 暗号化されていない場合はそのまま返却
            var privateKey = qrFormat.data.privateKey;
            if (privateKey != null) return privateKey;

            // 暗号化されたQRコードの場合は復号化する
            // @see https://github.com/symbol/qr-library/blob/main/src/services/EncryptionService.ts#L75-L113
            if (qrFormat.data.ciphertext != null && qrFormat.data.salt != null)
            {
                var password = await DisplayPromptAsync("Password", AppResources.BarcodeReader_GetPrivateKey_Discription, "OK", "Cancel", "Input Password", -1, Keyboard.Text);
                if (password == null) return null;

                var salt = StringToBytes(qrFormat.data.salt);
                var iv = StringToBytes(qrFormat.data.ciphertext.Substring(0, 32));
                var chiper = qrFormat.data.ciphertext.Substring(32);
                var key = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt, 2000, HashAlgorithmName.SHA1).GetBytes(32);
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;
                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var mstream = new MemoryStream(Convert.FromBase64String(chiper));
                await using var cstream = new CryptoStream(mstream, decryptor, CryptoStreamMode.Read);
                using var sreader = new StreamReader(cstream);
                try
                {
                    privateKey = await sreader.ReadToEndAsync();
                }
                catch (CryptographicException)
                {
                    await DisplayAlert("Failed", AppResources.LangUtil_FailedPassword, "OK");
                }
            }
            return privateKey;
        }
        catch
        {
            return null;
        }
    }

    static byte[] StringToBytes(string str)
    {
        var bs = new List<byte>();
        for (var i = 0; i < str.Length / 2; i++)
        {
            bs.Add(Convert.ToByte(str.Substring(i * 2, 2), 16));
        }
        return bs.ToArray();
    }

    void OnContentPageUnloaded(object sender, EventArgs e)
    {
        cameraView.StopCameraAsync();
        cameraView.CamerasLoaded -= CameraViewCamerasLoaded;
        cameraView.BarcodeDetected -= CameraViewBarcodeDetected;
    }

    private async void OnQRCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
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
        public string ciphertext { get; set; }
        public string salt { get; set; }
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