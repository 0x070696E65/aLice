using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using aLice.Models;
using SymbolSdk.Symbol;

namespace aLice.Services;

public class Symbol
{
    private readonly string Node;
    public Symbol(string node)
    {
        Node = node;
    }
    
    public async Task<bool> CheckNodeHealth()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(Node + "/node/health");
        if (!response.IsSuccessStatusCode) return false;
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(text);
        var status = json?["status"];
        if (status == null) return false;
        var apiNode = status["apiNode"];
        var db = status["db"];
        return apiNode?.ToString() == "up" && db?.ToString() == "up";
    }

    public async Task<string> CheckStatus(string hash)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(Node + "/transactionStatus/" + hash);
        Console.WriteLine(Node + "/transactionStatus/" + hash);
        if (!response.IsSuccessStatusCode) throw new Exception("Transaction not found");
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(text);
        if (json != null) return (string) json["code"];
        throw new Exception("json is not correct format");
    }

    public async Task<string> Announce(string payload, AnnounceType announceType)
    {
        using var client = new HttpClient();
        StringContent content;
        string endPoint;
        switch (announceType)
        {
            case AnnounceType.Bonded:
                content = new StringContent("{\"payload\": \"" + payload + "\"}", Encoding.UTF8, "application/json");
                endPoint = "/transactions/partial";
                break;
            case AnnounceType.Normal:
                content = new StringContent("{\"payload\": \"" + payload + "\"}", Encoding.UTF8, "application/json");
                endPoint = "/transactions";
                break;
            case AnnounceType.Cosignature:
                var arr = payload.Split("_");
                var data = new Dictionary<string, string>()
                {
                    {"parentHash", arr[0]},
                    {"signature", arr[1]},
                    {"signerPublicKey", arr[2]},
                    {"version", "0"}
                };
                var json = JsonSerializer.Serialize(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
                endPoint = "/transactions/cosignature";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(announceType), announceType, null);
        }
        var response =  client.PutAsync(Node + endPoint, content).Result;
        return await response.Content.ReadAsStringAsync();
    }
    
    public static (string hash, string address) GetHash(string payload)
    {
        var tx = TransactionFactory.Deserialize(payload);
        var facade = new SymbolFacade(tx.Network == NetworkType.MAINNET ? SymbolSdk.Symbol.Network.MainNet : SymbolSdk.Symbol.Network.TestNet);
        return (facade.HashTransaction(tx).ToString(), facade.Network.PublicKeyToAddress(tx.SignerPublicKey).ToString());
    }
}