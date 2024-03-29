using System.Collections.ObjectModel;
using System.Text.Json;
using aLice.Models;
using aLice.Resources;
using CatSdk.Facade;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace aLice.ViewModels;

public abstract class AccountViewModel
{
    public static SavedAccounts Accounts = new SavedAccounts();
    public static SavedAccount MainAccount;
    public static string[] AccountNames;
    public static int MemoryPasswordSeconds = 0;
    
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
            MemoryPasswordSeconds = await SecureStorage.GetAsync("MemoryPasswordSeconds") == null ? 0 : int.Parse(await SecureStorage.GetAsync("MemoryPasswordSeconds"));
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
            throw new Exception(AppResources.LangUtil_FailedPassword);
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
            throw new Exception(AppResources.LangUtil_FailedPassword);
        }
    }
    
    public static async Task ChangeMainAccount(string address)
    {
        Accounts.accounts.ToList().ForEach(acc => acc.isMain = acc.address == address);
        MainAccount = Accounts.accounts.ToList().Find(acc => acc.address == address);
        var updatedAccounts = JsonSerializer.Serialize(Accounts);
        SecureStorage.Remove("CurrentPassword");
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
    
    // アカウントの生体認証を判別する
    public static bool IsUseBionic(string address)
    {
        var element = Accounts.accounts.ToList().Find(acc => acc.address == address);
        return element.isBiometrics;
    }
    
    // アカウントの生体認証を更新する
    public static async Task UpdateUseBionic(string address, string privateKey, bool isUseBionic)
    {
        var elementsToUpdate = Accounts.accounts.ToList().Find(acc => acc.address == address);
        if (isUseBionic)
        {
            elementsToUpdate.isBiometrics = false;
            await SecureStorage.SetAsync($"{address}_privateKey", "");
        }
        else
        {
            elementsToUpdate.isBiometrics = true;
            await SecureStorage.SetAsync($"{address}_privateKey", privateKey);
        }
        var updatedAccounts = JsonSerializer.Serialize(Accounts);
        await SecureStorage.SetAsync("accounts", updatedAccounts);
    }
    
    public static async Task<string> ReturnPrivateKeyUsingBionic()
    {
        if (await CrossFingerprint.Current.IsAvailableAsync())
        {
            var request = new AuthenticationRequestConfiguration
                ("get privateKey using biometrics", "Confirm get privateKey with your biometrics");

            var result = await CrossFingerprint.Current.AuthenticateAsync(request);
            if (result.Authenticated)
            {
                return await SecureStorage.GetAsync($"{MainAccount.address}_privateKey");
            }
        }
        throw new Exception(AppResources.AccountViewModel_FailedBionic);
    }

    public static (bool isValid, string message) ValidationAccount(
        string Name, string Address, bool hasPrivateKey, byte[] publicKey, string Password, string NetworkType, bool isNewAccount = false
    )
    {
        var message = "";
        var isValid = true;

        if (Name == null)
        {
            message += $"{AppResources.AccountViewModel_RequiredAccountName}\n";
            isValid = false;
        }
        
        if (Password == null)
        {
            message += $"{AppResources.AccountViewModel_RequiredPassword}\n";
            isValid = false;
        }
        
        if (!isNewAccount)
        {
            if (Address == null)
            {
                message += $"{AppResources.AccountViewModel_RequiredAddress}\n";
                isValid = false;
            }
            
            if (!hasPrivateKey)
            {
                message += $"{AppResources.AccountViewModel_RequiredPrivateKey}\n";
                isValid = false;
            }

            try
            {
                var networkType = NetworkType switch
                {
                    "MainNet" => CatSdk.Symbol.Network.MainNet,
                    "TestNet" => CatSdk.Symbol.Network.TestNet,
                    _ => throw new Exception(AppResources.LangUtil_IncorrectNetwork)
                };
                var facade = new SymbolFacade(networkType);

                // 入力された秘密鍵とアドレスが一致するか確認
                var address = facade.Network.PublicKeyToAddress(publicKey);

                if (address.ToString() != Address)
                {
                    message += $"{AppResources.AccountViewModel_NotMatchPrivateKeyAndAddress}\n";
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
                    message += $"{AppResources.AccountViewModel_AccountAlreadyRegistered}\n";
                    isValid = false;
                }

                if (savedAccount.address == Address)
                {
                    message += $"{AppResources.AccountViewModel_AddressAlreadyRegistered}\n";
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

    private readonly static List<CancellationTokenSource> cancellationTokenSources = new List<CancellationTokenSource>();
    public static async ValueTask DeletePassword()
    {
        if (cancellationTokenSources.Count > 0)
        {
            foreach (var c in cancellationTokenSources)
            {
                c.Cancel();   
            }
        }
        cancellationTokenSources.Clear();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSources.Add(cancellationTokenSource);
        await ActionDeletePassword(cancellationTokenSource.Token);
    }
    
    private static async Task ActionDeletePassword(CancellationToken token)
    {
        await Task.Delay(MemoryPasswordSeconds * 1000, token);
        token.ThrowIfCancellationRequested();
        SecureStorage.Remove("CurrentPassword");
    }

    public static void DeletePasswordByTimestamp()
    {
        try
        {
            var p = SecureStorage.GetAsync("CurrentPassword").Result.Split("_");
            var memoryPasswordSeconds = int.Parse(SecureStorage.GetAsync("MemoryPasswordSeconds").Result);
            if (long.Parse(p[1]) + memoryPasswordSeconds < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                SecureStorage.Remove("CurrentPassword");
            }
        }
        catch
        {
            // 念のため削除
            SecureStorage.Remove("CurrentPassword");
        }
    }
}