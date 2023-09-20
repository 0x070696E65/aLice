namespace aLice;

public class SavedAccounts
{
    public List<SavedAccount> accounts { get; set; }
}

public class SavedAccount
{
    public bool isMain { get; set; }
    public string accountName { get; set; }
    public string address { get; set; }
    public string publicKey { get; set; }
    public string encryptedPrivateKey { get; set; }
    public string networkType { get; set; }
    
}