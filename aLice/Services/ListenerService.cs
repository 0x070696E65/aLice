using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

namespace aLice.Services;

public class ListenerService
{
    private readonly string uri;
    private readonly ClientWebSocket webSocket;
    private string uid;
    
    public ListenerService(string _uri, ClientWebSocket _webSocket)
    {
        if(_uri.Contains("http"))
            _uri = _uri.Replace("http", "ws") + "/ws";
        if(_uri.Contains("https"))
            _uri = _uri.Replace("https", "wss") + "/ws";
        uri = _uri;
        webSocket = _webSocket;
        uid = "";
    }
    
    public async Task Open()
    {
        Console.WriteLine(uri);
        var serverUri = new Uri(uri);
        await webSocket.ConnectAsync(serverUri, CancellationToken.None);
        Console.WriteLine(@"Connected to Symbol node WebSocket server.");

        var buffer = new byte[256];
        var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        var json = JsonNode.Parse(receivedMessage);
        uid = (string)json["uid"];
    }
    
    public async Task<bool> NewBlock(Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"block\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[4096];
                var receiveResult =
                    await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var json = JsonNode.Parse(receivedMessage);
                if ((string) json["topic"] == "block")
                    callback?.Invoke(json["data"]);
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            // キャンセルトークンが要求された場合、OperationCanceledExceptionがスローされる可能性があります
            Close();
            return false;
        }
    }
    
    public async Task Confirmed(string address, CancellationToken token, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"confirmedAdded/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[4096];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var json = JsonNode.Parse(receivedMessage);
                if((string)json["topic"] == "confirmedAdded/" + address)
                    callback?.Invoke(json["data"]);

                if (!token.IsCancellationRequested) continue;
                Close();
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルトークンが要求された場合、OperationCanceledExceptionがスローされる可能性があります
            Close();
        }
    }
    
    public async Task Status(string address, CancellationToken token, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"status/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[4096];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                var json = JsonNode.Parse(receivedMessage);
                if((string)json["topic"] == "status/" + address)
                    callback?.Invoke(json["data"]);
                
                if (!token.IsCancellationRequested) continue;
                Close();
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルトークンが要求された場合、OperationCanceledExceptionがスローされる可能性があります
            Close();
        }
    }
    
    public async Task Unconfirmed(string address, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"unconfirmedAdded/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            var json = JsonNode.Parse(receivedMessage);
            if((string)json["topic"] == "unconfirmedAdded/" + address)
                callback?.Invoke(json["data"]);
        }
    }
    
    public async Task AggregateBondedAdded(string address, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"partialAdded/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            var json = JsonNode.Parse(receivedMessage);
            if((string)json["topic"] == "partialAdded/" + address)
                callback?.Invoke(json["data"]);
        }
    }
    
    private async Task PartialRemoved(string address, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"partialRemoved/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            var json = JsonNode.Parse(receivedMessage);
            if((string)json["topic"] == "partialRemoved/" + address)
                callback?.Invoke(json["data"]);
        }
    }
    
    private async Task Cosignature(string address, Action<JsonNode> callback = null)
    {
        var body = Encoding.UTF8.GetBytes("{\"uid\":\"" + uid + "\", \"subscribe\":\"cosignature/" + address + "\"}");
        await webSocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Text, true, CancellationToken.None);
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            var json = JsonNode.Parse(receivedMessage);
            if((string)json["topic"] == "cosignature/" + address)
                callback?.Invoke(json["data"]);
        }
    }

    public void Close(string reason = "Closing")
    {
        if (webSocket.State == WebSocketState.Closed)
        {
            Console.WriteLine("websocket is already closed");
            return;
        }
        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
        Console.WriteLine($@"Closed WebSocket connection. uid:{uid}");
        webSocket.Dispose();
    }
}