using CatSdk.Utils;

namespace aLice.Models;

public class NotificationModel
{
    public readonly string Data;
    public readonly RequestType RequestType;
    public readonly string Method;
    public readonly string RedirectUrl;
    public readonly string CallbackUrl;
    public readonly string BaseUrl;
    public readonly string RecipientPublicKeyForEncryptMessage;
    public readonly string FeeMultiplier;
    public readonly List<string> Batches = new();
    public readonly string SetPublicKey;
    public readonly string Node;
    public readonly string Deadline;
    public readonly SdkVersion SdkVersion = SdkVersion.V3;
    public NotificationModel(string _uri)
    {
        var queryString = _uri.Split('?').LastOrDefault();
        if (queryString == null) return;
        var dict = queryString.Split('&')
            .Select(s => s.Split('='))
            .ToDictionary(a => a[0], a => a[1]);
        var hasType = dict.TryGetValue("type", out var _type);
        
        if (!hasType)
        {
            throw new Exception("type is null");   
        }
        
        RequestType = _type switch
        {
            "request_sign_utf8" => RequestType.SignUtf8,
            "request_sign_transaction" => RequestType.SignTransaction,
            "request_sign_binary_hex" => RequestType.SignBinaryHex,
            "request_pubkey" => RequestType.Pubkey,
            "request_sign_batches" => RequestType.Batches,
            _ => throw new Exception("type is invalid")
        };
        
        var hasData = dict.TryGetValue("data", out var _data);
        if (hasData)
        {
            Data = _data;
        }
        
        var hasCallbackUrl = dict.TryGetValue("callback", out var _callbackUrl);
        var hasMethod = dict.TryGetValue("method", out var _method);
        Method = hasMethod ? _method : "get";
        
        if (!hasData && RequestType != RequestType.Pubkey)
        {
            throw new Exception("data is null");
        }
        
        var hasRecipientPublicKeyForEncryptMessage = dict.TryGetValue("recipient_publicKey_for_encrypt_message", out var _recipientPublicKeyForEncryptMessage);
        if (hasRecipientPublicKeyForEncryptMessage)
        {
            RecipientPublicKeyForEncryptMessage = _recipientPublicKeyForEncryptMessage;
        }
        var hasFeeMultiplier = dict.TryGetValue("fee_multiplier", out var _feeMultiplier);
        if (hasFeeMultiplier)
        {
            FeeMultiplier = _feeMultiplier;
        }

        var hasRedirectUrl = dict.TryGetValue("redirect_url", out var _redirectUrl);
        if (hasRedirectUrl) {
            RedirectUrl = Converter.HexToUtf8(_redirectUrl);   
        }
        
        if (hasCallbackUrl)
        {
            CallbackUrl = Converter.HexToUtf8(_callbackUrl);
            var uri = new Uri(CallbackUrl);
            BaseUrl = $"{uri.Scheme}://{uri.Authority}";
        }

        if (RequestType == RequestType.Batches)
        {
            var count = 0;
            while (true)
            {
                var hasBatches = dict.TryGetValue("batch" + count, out var metal);
                if (!hasBatches)
                {
                    break;
                }
                Batches.Add(metal);
                count++;
            }   
        }
        
        var hasSetPublicKey = dict.TryGetValue("set_public_key", out var _setPublicKey);
        if (hasSetPublicKey)
        {
            SetPublicKey = _setPublicKey;
        }
        
        var hasDeadline = dict.TryGetValue("deadline", out var _deadline);
        if (hasDeadline)
        {
            Deadline = _deadline;
        }
        
        var hasNode = dict.TryGetValue("node", out var _node);
        if (hasNode)
        {
            Node = Converter.HexToUtf8(_node);
        }
        
        var hasSdkVersion = dict.TryGetValue("sdk_version", out var sdkVersion);
        if (hasSdkVersion)
        {
            if (sdkVersion == "v2")
            {
                SdkVersion = SdkVersion.V2;
            }
        }
    }
}