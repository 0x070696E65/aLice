using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using aLice.Models;
using aLice.Services;
using CatSdk.Crypto;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice.ViewModels;

public abstract class RequestViewModel
{
    public static NotificationModel Notification { get; private set; }
    public static readonly List<(IBaseTransaction transaction, string parsedTransaction)> ParseTransaction = new ();
    private static byte[] BytesData;
    private static readonly string RejectParam = "error=sign_rejected";
    
    public static void SetNotification(NotificationModel notification)
    {
        Notification = notification;
    }

    public static (string domainText, string typeText, string dataText, string askText) GetShowTexts()
    {
        var domainText = "";
        var typeText = "";
        var dataText = "";
        if (Notification.CallbackUrl != null)
        {
            var uri = new Uri(Notification.CallbackUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Authority}";
            domainText = $"{baseUrl}からの署名要求です";    
        }
        if(Notification.RequestType == RequestType.SignUtf8)
        {
            typeText = "UTF8文字列です";
            dataText = Converter.HexToUtf8(Notification.Data);
            BytesData = Converter.HexToBytes(Notification.Data);
        } else if(Notification.RequestType == RequestType.SignBinaryHex)
        {
            typeText = "バイナリデータの16進数文字列です";
            dataText = Notification.Data;
            BytesData = Converter.HexToBytes(Notification.Data);
        }
        else if (Notification.RequestType == RequestType.SignTransaction)
        {
            ParseTransaction.Clear();
            ParseTransaction.Add(SymbolTransaction.ParseTransaction(Notification.Data, Notification.RecipientPublicKeyForEncryptMessage, Notification.FeeMultiplier, Notification.Deadline));
            
            SetMainAccountSignerPublicKey();
            
            typeText = "Symbolのトランザクションです";
            dataText = ParseTransaction[0].parsedTransaction;

            if (Notification.RecipientPublicKeyForEncryptMessage != null && ParseTransaction[0].transaction.Type == TransactionType.TRANSFER)
            {
                typeText += "\nメッセージの暗号化を行います。";
            }
        }
        else if (Notification.RequestType == RequestType.Batches)
        {
            foreach (var s in Notification.Batches)
            {
                var tx = SymbolTransaction.ParseEmbeddedTransaction(s);
                tx.transaction.SignerPublicKey = new PublicKey(Converter.HexToBytes(AccountViewModel.MainAccount.publicKey));
                dataText += tx.parsedTransaction;
                ParseTransaction.Add(tx);
            }
            typeText = "複数のトランザクションです";
        }
        var askText = $"{AccountViewModel.MainAccount.accountName}で署名しますか？";
        return (domainText, typeText, dataText, askText);
    }

    public static async Task<(ResultType resultType, string result)> Accept(string password = "")
    {
        return Notification.RequestType switch
        {
            RequestType.Pubkey => AcceptRequestPublicKey(),
            RequestType.SignTransaction or RequestType.SignUtf8 or RequestType.SignBinaryHex =>
                await AcceptRequestSign(password),
            RequestType.Batches => await AcceptRequestBatches(password),
            _ => throw new Exception("不正なリクエストです")
        };
    }

    public static (bool isCallBack, string result) Reject()
    {
        if (Notification.CallbackUrl == null)
        {
            return (false, "");
        }
        
        var callbackUrl = Notification.CallbackUrl;
        if (Notification.CallbackUrl.Contains('?')) {
            callbackUrl += "&" + RejectParam;
        }
        else {
            callbackUrl += "?" + RejectParam;
        }
        return (true, callbackUrl);
    }
    
    private static (ResultType resultType, string result) AcceptRequestPublicKey()
    {
        if (Notification.CallbackUrl == null)
        {
            return (ResultType.ShowData, AccountViewModel.MainAccount.publicKey);
        }
        
        var callbackUrl = "";
        if (Notification.CallbackUrl.Contains('?')) {
            callbackUrl += $"{Notification.CallbackUrl}&pubkey={AccountViewModel.MainAccount.publicKey}&network={AccountViewModel.MainAccount.networkType}";
        }
        else {
            callbackUrl += $"{Notification.CallbackUrl}?pubkey={AccountViewModel.MainAccount.publicKey}&network={AccountViewModel.MainAccount.networkType}";
        }
        return (ResultType.Callback, callbackUrl);
    }

    private static async Task<(ResultType resultType, string result)> AcceptRequestSign(string password)
    {
        string privateKey;
        try
        {
            privateKey = GetPrivateKey(password);
            if (AccountViewModel.MemoryPasswordSeconds != 0)
            {
                await SecureStorage.SetAsync("CurrentPassword", $"{password}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            }
        } catch
        {
            throw new Exception("パスワードが間違っています");
        }
        var keyPair = new KeyPair(new PrivateKey(privateKey));

        if (Notification.RequestType == RequestType.SignTransaction)
        {
            if (Notification.RecipientPublicKeyForEncryptMessage != null &&
                ParseTransaction[0].transaction.Type == TransactionType.TRANSFER)
            {
                var transferTransaction = ParseTransaction[0].transaction as TransferTransactionV1;
                var bytes = transferTransaction?.Message;
                if (bytes is {Length: >= 2})
                {
                    if (Notification.SdkVersion == SdkVersion.V2)
                    {
                        var message = Encoding.UTF8.GetString(bytes.ToList().GetRange(1, bytes.Length - 1).ToArray());
                        var encrypted = Crypto.Encode(privateKey, Notification.RecipientPublicKeyForEncryptMessage, message);
                        var m = Encoding.UTF8.GetBytes(encrypted);
                        var zero = new byte[] { 1 };
                        var newArr = new byte[m.Length + 1];
                        zero.CopyTo(newArr, 0);
                        m.CopyTo(newArr, 1);
                        Console.WriteLine(Converter.BytesToHex(newArr));
                        ((TransferTransactionV1) ParseTransaction[0].transaction).Message = newArr;
                        TransactionHelper.SetMaxFee(transferTransaction, Notification.FeeMultiplier != null ? int.Parse(Notification.FeeMultiplier) : 100);
                    }
                    else
                    {
                        var message = Encoding.UTF8.GetString(bytes.ToList().GetRange(1, bytes.Length - 1).ToArray());
                        var encrypted = "01" + Crypto.Encode(privateKey, Notification.RecipientPublicKeyForEncryptMessage, message);
                        Console.WriteLine(encrypted);
                        ((TransferTransactionV1) ParseTransaction[0].transaction).Message = Converter.HexToBytes(encrypted);
                        TransactionHelper.SetMaxFee(transferTransaction, Notification.FeeMultiplier != null ? int.Parse(Notification.FeeMultiplier) : 100);
                    }
                }
            }

            var network = ParseTransaction[0].transaction.Network == CatSdk.Symbol.NetworkType.MAINNET
                ? CatSdk.Symbol.Network.MainNet
                : CatSdk.Symbol.Network.TestNet;
            var facade = new CatSdk.Facade.SymbolFacade(network);
            var signature = facade.SignTransaction(keyPair, ParseTransaction[0].transaction);
            var signedTransaction =
                CatSdk.Symbol.Factory.TransactionsFactory.AttachSignatureTransaction((ITransaction)ParseTransaction[0].transaction,
                    signature);
            var signedPayload = Converter.BytesToHex(signedTransaction.Serialize());

            switch (Notification.Method)
            {
                case "post":
                    var dic = new Dictionary<string, string>
                    {
                        {"pubkey", AccountViewModel.MainAccount.publicKey},
                        {"original_data", Notification.Data},
                        {"signed_payload", signedPayload},
                    };
                    return await Post(dic);
                case "get":
                    return Get(signedPayload, "signed_payload");
                case "announce":
                    return await Announce(signedPayload);
                case "announce_bonded":
                    return await Announce(signedPayload, true);
                default:
                    throw new Exception("不正なリクエストです");
            }
        }
        else
        {
            var signature = keyPair.Sign(BytesData);
            switch (Notification.Method)
            {
                case "post":
                    var dic = new Dictionary<string, string>
                    {
                        {"pubkey", AccountViewModel.MainAccount.publicKey},
                        {"original_data", Notification.Data},
                        {"signature", Converter.BytesToHex(signature.bytes)},
                    };
                    return await Post(dic);
                case "get":
                    return Get(Converter.BytesToHex(signature.bytes), "signature");
                default:
                    throw new Exception("不正なリクエストです");
            }
        }
    }

    private static async Task<(ResultType resultType, string result)> AcceptRequestBatches(string password)
    {
        string privateKey;
        try
        {
            privateKey = GetPrivateKey(password);
        }
        catch
        {
            throw new Exception("パスワードが間違っています");
        }

        var keyPair = new KeyPair(new PrivateKey(privateKey));
        
        var network = ParseTransaction[0].transaction.Network == CatSdk.Symbol.NetworkType.MAINNET ? CatSdk.Symbol.Network.MainNet : CatSdk.Symbol.Network.TestNet;
        var metal = new Services.Metal(network);
        var txs = ParseTransaction.Select(valueTuple => valueTuple.transaction).ToList();
            
        var aggs = metal.SignedAggregateCompleteTxBatches(txs, keyPair, network);
        switch (Notification.Method)
        {
            case "post":
            {
                var dic = new Dictionary<string, string> {{"pubkey", AccountViewModel.MainAccount.publicKey}};
                for (var i = 0; i < aggs.Count; i++)
                {
                    dic.Add("signed" + i, Converter.BytesToHex(aggs[i].Serialize()));   
                }
                return await Post(dic);
            }
            case "get":
            {
                var callbackUrl = "";
                var additionalParam = $"pubkey={AccountViewModel.MainAccount.publicKey}";
                callbackUrl = Notification.CallbackUrl.Contains('?') ? $"{Notification.CallbackUrl}&{additionalParam}" : $"{Notification.CallbackUrl}?{additionalParam}";
                for (var i = 0; i < aggs.Count; i++)
                {
                    var signedPayload = Converter.BytesToHex(aggs[i].Serialize());
                    callbackUrl += $"&signed{i}={signedPayload}";
                }
                return (ResultType.Callback, callbackUrl);
            }
            default:
                throw new Exception("不正なリクエストです");
        }
    }

    private static async Task<(ResultType resultType, string result)> Post(Dictionary<string, string> dic)
    {
        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(dic), Encoding.UTF8, "application/json");
        var response = client.PostAsync(Notification.CallbackUrl, content).Result;
        await response.Content.ReadAsStringAsync();
        return Notification.RedirectUrl != null ? (ResultType.Callback, Notification.RedirectUrl) : (ResultType.ShowData, "");
    }

    private static (ResultType resultType, string result) Get(string signedContent, string contentKey)
    {
        if (Notification.CallbackUrl == null) return (ResultType.ShowData, signedContent);
        var callbackUrl = "";
        var additionalParam =
            $"{contentKey}={signedContent}&original_data={Notification.Data}&pubkey={AccountViewModel.MainAccount.publicKey}&network={AccountViewModel.MainAccount.networkType}";
        if (Notification.CallbackUrl.Contains('?'))
        {
            callbackUrl += $"{Notification.CallbackUrl}&{additionalParam}";
        }
        else
        {
            callbackUrl += $"{Notification.CallbackUrl}?{additionalParam}";
        }

        return (ResultType.Callback, callbackUrl);
    }

    private static async Task<(ResultType resultType, string result)> Announce(string signedPayload, bool isBonded = false)
    {
        var symbolService = new Symbol(Notification.Node);
        if (!await symbolService.CheckNodeHealth())
        {
            throw new Exception("指定されたノードがダウンしているか、正しいノードURLではありません");
        };
        try
        {
            return isBonded ? (ResultType.AnnounceBonded, signedPayload) : (ResultType.Announce, signedPayload);
        }
        catch
        {
            throw new Exception("アナウンスに失敗しました");
        }
    }
    
    public static SavedAccount GetRequestAccount()
    {
        var list = AccountViewModel.Accounts.accounts.ToList().Find(acc => acc.publicKey == Notification.SetPublicKey);
        if(list == null) throw new Exception("指定されたアカウントが存在しません");
        return list;
    }
    
    private static string GetPrivateKey(string password)
    {
        return CatSdk.Crypto.Crypto.DecryptString(AccountViewModel.MainAccount.encryptedPrivateKey, password, AccountViewModel.MainAccount.address);
    }

    public static void SetMainAccountSignerPublicKey()
    {
        ParseTransaction[0].transaction.SignerPublicKey = new PublicKey(Converter.HexToBytes(AccountViewModel.MainAccount.publicKey));
        
        // もしトランザクションがアグリゲートでかつ公開鍵が空なら内部トランザクションの署名者をメインアカウントに変更する
        if (ParseTransaction[0].transaction is AggregateCompleteTransactionV2)
        {
            var aggregateTx = ParseTransaction[0].transaction as AggregateCompleteTransactionV2;
            var baseTransactions = aggregateTx?.Transactions;
            if (baseTransactions != null)
            {
                foreach (var tx in baseTransactions)
                {
                    if (Converter.BytesToHex(tx.SignerPublicKey.bytes) == "0000000000000000000000000000000000000000000000000000000000000000")
                    {
                        tx.SignerPublicKey =  new PublicKey(Converter.HexToBytes(AccountViewModel.MainAccount.publicKey));
                    }
                }
                var merkleHash = SymbolFacade.HashEmbeddedTransactions(baseTransactions);
                aggregateTx.TransactionsHash = merkleHash;
            }
        }
        Console.WriteLine(Converter.BytesToHex(ParseTransaction[0].transaction.Serialize()));
        
        if (ParseTransaction[0].transaction is AggregateBondedTransactionV2)
        {
            var aggregateTx = ParseTransaction[0].transaction as AggregateCompleteTransactionV2;
            var baseTransactions = aggregateTx?.Transactions;
            if (baseTransactions != null)
            {
                foreach (var tx in baseTransactions)
                {
                    if (tx.SignerPublicKey.bytes ==
                        Converter.HexToBytes("0000000000000000000000000000000000000000000000000000000000000000"))
                    {
                        tx.SignerPublicKey =  new PublicKey(Converter.HexToBytes(AccountViewModel.MainAccount.publicKey));
                    }
                }   
                var merkleHash = SymbolFacade.HashEmbeddedTransactions(baseTransactions);
                aggregateTx.TransactionsHash = merkleHash;
            }
        }
    }
}