using System.Collections.ObjectModel;
using System.ComponentModel;

namespace aLice.Models;

public class SavedAccounts
{
    public ObservableCollection<SavedAccount> accounts { get; set; }
}

public sealed class SavedAccount: INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    public string accountName { get; set; }
    public string address { get; set; }
    public string publicKey { get; set; }
    public string encryptedPrivateKey { get; set; }
    public string networkType { get; set; }
    public bool isBiometrics { get; set; }

    private bool _isMain { get; set; }
    
    public bool isMain
    {
        get => _isMain;
        set
        {
            if (_isMain == value) return;
            _isMain = value;
            OnPropertyChanged(nameof(isMain));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}