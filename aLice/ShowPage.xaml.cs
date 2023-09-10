using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aLice;

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
        await Application.Current.MainPage.DisplayAlert("Copied", "クリップボードにコピーしました", "閉じる");
    }
    
    private async void OnClose(object sender, EventArgs e)
    {
        await Application.Current.MainPage.Navigation.PopModalAsync();
    }
}