using aLice.Resources;

namespace aLice.Views;

public partial class ShowPage : ContentPage
{
    public ShowPage(string title, string data)
    {
        InitializeComponent();
        ShowData.Text = data;
        Title.Text = title;
    }

    private async void OnCopyData(object sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(ShowData.Text);
        await Application.Current.MainPage.DisplayAlert("Copied", AppResources.LangUtil_CopiedToClipBoard, AppResources.LangUtil_Close);
    }
    
    private async void OnClose(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}