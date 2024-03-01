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

                if (qrCodeText.StartsWith("alice://sign?"))
                {
                    if (Navigation.ModalStack.Count > 0)
                    {
                        await Navigation.PopModalAsync();
                    }

                    App.RequestNotification(qrCodeText, new CancellationToken());
                }
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
