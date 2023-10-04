using System.Text.Json;
using System.Text.Json.Nodes;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;

namespace aLice.Views;

public partial class QrReaderForSign : ContentPage
{
    private HashSet<string> scannedQRCodes = new HashSet<string>();

    public QrReaderForSign()
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
            try
            {
                var qrCodeText = args.Result[0].Text;
                if (scannedQRCodes.Contains(qrCodeText)) return;
                scannedQRCodes.Add(qrCodeText);
                
                var url = "aLice://sign?";
                var jsonDocument = JsonDocument.Parse(args.Result[0].Text);
                if (!jsonDocument.RootElement.TryGetProperty("alice", out var aliceObject)) return;
                    
                var d = aliceObject.EnumerateObject().ToArray();
                for (var i = 0; i < d.Count(); i++)
                {
                    var propertyName = d[i].Name;
                    var propertyValue = d[i].Value;
                    url += i == 0 ? $"{propertyName}={propertyValue}" : $"&{propertyName}={propertyValue}";
                }

                if (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync();
                }

                App.RequestNotification(url, new CancellationToken());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        });
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
}
