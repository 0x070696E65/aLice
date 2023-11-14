using System.Globalization;
using aLice.ViewModels;

namespace aLice.Views;

public partial class Settings : ContentPage
{
    private int memoryPasswordSeconds;
    public Settings()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MemoryTimeSlider.Value = AccountViewModel.MemoryPasswordSeconds;
    }

    private void MemoryTimeSliderChanged(object sender, ValueChangedEventArgs e)
    {
        var slider = (Slider) sender;
        memoryPasswordSeconds = (int) Math.Round(slider.Value / 10) * 10;
        slider.Value = memoryPasswordSeconds;
        MemoryTimeValue.Text = memoryPasswordSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private async void OnSaveMemoryTime(object sender, EventArgs e)
    {
        AccountViewModel.MemoryPasswordSeconds = memoryPasswordSeconds;
        await SecureStorage.SetAsync("MemoryPasswordSeconds", AccountViewModel.MemoryPasswordSeconds.ToString(CultureInfo.InvariantCulture));
        await Navigation.PopModalAsync();
    }

    private async void OnClickedCliseButton(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}