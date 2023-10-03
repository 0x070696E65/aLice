using System.Globalization;
using aLice.ViewModels;

namespace aLice.Views;

public partial class Settings : ContentPage
{
    private int memoryPasswordMilliseconds;
    public Settings()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MemoryTimeSlider.Value = AccountViewModel.MemoryPasswordMilliseconds / 1000;
    }

    private void MemoryTimeSliderChanged(object sender, ValueChangedEventArgs e)
    {
        var slider = (Slider) sender;
        memoryPasswordMilliseconds = (int) Math.Round(slider.Value / 10) * 10;
        slider.Value = memoryPasswordMilliseconds;
        MemoryTimeValue.Text = memoryPasswordMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    private async void OnSaveMemoryTime(object sender, EventArgs e)
    {
        AccountViewModel.MemoryPasswordMilliseconds = memoryPasswordMilliseconds * 1000;
        await SecureStorage.SetAsync("MemoryPasswordMilliseconds", AccountViewModel.MemoryPasswordMilliseconds.ToString(CultureInfo.InvariantCulture));
        await Navigation.PopModalAsync();
    }

    private async void OnClickedCliseButton(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}