using System.Text.Json;
using CatSdk.Facade;

namespace aLice.ViewModels;

public abstract class AccountViewModel
{
    public static SavedAccounts Accounts { get; private set; }
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
            MainAccount = Accounts.accounts.Find((acc) => acc.isMain);
        }
        catch
        {
            Accounts = new SavedAccounts();
        }
    }
    
    public static void ChangeMainAccount(string name)
    {
        MainAccount = Accounts.accounts.Find(acc => acc.accountName == name);
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

    public static async Task<(bool isValid, string message)> ValidationAccount(
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
            // 保存されているアドレスを取得
            var accounts = await SecureStorage.GetAsync("accounts");
            var savedAccounts = JsonSerializer.Deserialize<SavedAccounts>(accounts);
            foreach (var savedAccount in savedAccounts.accounts)
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