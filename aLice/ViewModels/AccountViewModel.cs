using System.Collections.ObjectModel;
using System.Text.Json;
using aLice.Models;
using CatSdk.Facade;

namespace aLice.ViewModels;

public abstract class AccountViewModel
{
    public static SavedAccounts Accounts = new SavedAccounts();
    public static SavedAccount MainAccount;
    public static string[] AccountNames;
    public static async Task SetAccounts()
    {
        try
        {
            var accounts = await SecureStorage.GetAsync("accounts");
            Accounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            AccountNames = new string[Accounts.accounts.Count];
            for (var i = 0; i < Accounts.accounts.Count; i++)
            {
                AccountNames[i] = Accounts.accounts[i].accountName;
            }

            MainAccount = Accounts.accounts.ToList().Find((acc) => acc.isMain);
        }
        catch
        {
            Accounts = new SavedAccounts
            {
                accounts = new ObservableCollection<SavedAccount>()
            };   
        }
    }

    public static string ExportAccount(string address, string password)
    {
        try {
            var acc = Accounts.accounts.ToList().Find(acc => acc.address == address);
            return CatSdk.Crypto.Crypto.DecryptString(acc.encryptedPrivateKey, password, acc.address);
        }
        catch {
            throw new Exception("パスワードが正しくありません");
        }
    }
    
    public static async Task DeleteAccount(string address)
    {
        try {
            var elementsToRemove = Accounts.accounts.ToList().Find(acc => acc.address == address);
            Accounts.accounts.Remove(elementsToRemove);
            if (elementsToRemove.isMain && Accounts.accounts.Count > 0)
                Accounts.accounts[0].isMain = true;
            
            var updatedAccounts = JsonSerializer.Serialize(Accounts);
            await SecureStorage.SetAsync("accounts", updatedAccounts);
        }
        catch {
            throw new Exception("パスワードが正しくありません");
        }
    }
    
    public static void SetMainAccount(string address)
    {
        MainAccount = Accounts.accounts.ToList().Find(acc => acc.address == address);
    }
    
    public static async Task ChangeMainAccount(string address)
    {
        Accounts.accounts.ToList().ForEach(acc => acc.isMain = acc.address == address);
        Accounts.accounts = Accounts.accounts;
        var updatedAccounts = JsonSerializer.Serialize(Accounts);
        await SecureStorage.SetAsync("accounts", updatedAccounts);
    }
    
    // アカウントを保存する
    public static async Task SaveAccount(string name, string address, string publicKey, string encryptedPrivateKey, string networkType)
    {
        var savedAccount = new SavedAccount()
        {
            isMain = false,
            accountName = name,
            address = address,
            publicKey = publicKey,
            encryptedPrivateKey = encryptedPrivateKey,
            networkType = networkType
        };
        
        // 保存されているアカウントがない場合はメインアカウントにする        
        if (Accounts.accounts.Count == 0)
        {
            savedAccount.isMain = true;   
        }
        // 保存されているアドレスに追加
        Accounts.accounts.Add(savedAccount);
        // 保存
        await SecureStorage.SetAsync("accounts", JsonSerializer.Serialize(Accounts));
    }

    public static (bool isValid, string message) ValidationAccount(
        string Name, string Address, bool hasPrivateKey, byte[] publicKey, string Password, string NetworkType, bool isNewAccount = false
    )
    {
        var message = "";
        var isValid = true;

        if (Name == null)
        {
            message += "アカウント名は必須です\n";
            isValid = false;
        }
        
        if (Password == null)
        {
            message += "パスワードは必須です\n";
            isValid = false;
        }
        
        if (!isNewAccount)
        {
            if (Address == null)
            {
                message += "アドレスは必須です\n";
                isValid = false;
            }
            
            if (!hasPrivateKey)
            {
                message += "秘密鍵は必須です\n";
                isValid = false;
            }

            try
            {
                var networkType = NetworkType switch
                {
                    "MainNet" => CatSdk.Symbol.Network.MainNet,
                    "TestNet" => CatSdk.Symbol.Network.TestNet,
                    _ => throw new Exception("NetworkTypeが正しくありません")
                };
                var facade = new SymbolFacade(networkType);

                // 入力された秘密鍵とアドレスが一致するか確認
                var address = facade.Network.PublicKeyToAddress(publicKey);

                if (address.ToString() != Address)
                {
                    message += "秘密鍵とアドレスが一致しません\n";
                    isValid = false;
                }
            }
            catch (Exception e)
            {
                message += $"{e.Message}\n";
                isValid = false;
            }   
        }

        try
        {
            foreach (var savedAccount in Accounts.accounts)
            {
                if (savedAccount.accountName == Name)
                {
                    message += "アカウント名はすでに登録されています\n";
                    isValid = false;
                }

                if (savedAccount.address == Address)
                {
                    message += "アドレスはすでに登録されています\n";
                    isValid = false;
                }
            }
        }
        catch
        {
            // 保存されているアドレスがない場合は何もしない
        }

        return (isValid, message);
    }
}