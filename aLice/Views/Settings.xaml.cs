using System.Globalization;
using aLice.ViewModels;

namespace aLice.Views;

public partial class Settings : ContentPage
{
    private int memoryPasswordSeconds;
    private string language;
    public Settings()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MemoryTimeSlider.Value = AccountViewModel.MemoryPasswordSeconds;
        LanguagePicker.SelectedIndex = SecureStorage.GetAsync("Language").Result == "en" ? 1 : 0;
    }

    private void MemoryTimeSliderChanged(object sender, ValueChangedEventArgs e)
    {
        var slider = (Slider) sender;
        memoryPasswordSeconds = (int) Math.Round(slider.Value / 10) * 10;
        slider.Value = memoryPasswordSeconds;
        MemoryTimeValue.Text = memoryPasswordSeconds.ToString(CultureInfo.InvariantCulture);
    }
    
    private void OnLanguagePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var selectedIndex = picker.SelectedIndex;
        
        if (selectedIndex != -1)
        {
            // 選択された言語に基づいてカルチャを設定
            switch (picker.ItemsSource[selectedIndex])
            {
                case "日本語":
                    CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
                    CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
                    language = "ja";
                    break;
                case "English":
                    CultureInfo.CurrentCulture = new CultureInfo("en-US");
                    CultureInfo.CurrentUICulture = new CultureInfo("en-US");
                    language = "en";
                    break;
            }
        }
    }

    private async void OnSave(object sender, EventArgs e)
    {
        AccountViewModel.MemoryPasswordSeconds = memoryPasswordSeconds;
        await SecureStorage.SetAsync("MemoryPasswordSeconds", AccountViewModel.MemoryPasswordSeconds.ToString(CultureInfo.InvariantCulture));
        await SecureStorage.SetAsync("Language", language);
        await Navigation.PopModalAsync();
    }

    private async void OnClickedCliseButton(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}