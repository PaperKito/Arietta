using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Helpers;







public sealed class CdpTokenSniffer : IAsyncDisposable
{
    private const string TargetHost = "api.kurobbs.com";

    private ClientWebSocket? _ws;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, string> _trackedRequests = [];   
    private readonly object _gate = new();
    private int _nextCommandId = 1;

    public event Action<string>? Log;
    public event Action<string>? DidCaptured;
    public event Action<string>? TokenCaptured;
    public event Action<string>? UserIdCaptured;
    public event Action? Disconnected;


    
    public async Task ConnectAsync(string webSocketDebuggerUrl)
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(webSocketDebuggerUrl), _cts.Token);

        await SendCommandAsync("Network.enable", JsonDocument.Parse("""
            { "maxTotalBufferSize": 104857600, "maxResourceBufferSize": 10485760, "maxPostDataSize": 1048576 }
            """).RootElement.Clone());

        
        try
        {
            await SendCommandAsync("Page.reload", JsonDocument.Parse("""{ "ignoreCache": true }""").RootElement.Clone());
        }
        catch (Exception e)
        {
            Log?.Invoke($"页面刷新失败（不影响抓取，可手动切换页面）：{e.Message}");
        }

        Log?.Invoke($"CDP 已连接：{webSocketDebuggerUrl}");
        _ = ReceiveLoopAsync();
    }


    private async Task SendCommandAsync(string method, JsonElement? parameters = null)
    {
        var ws = _ws;
        if (ws?.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("CDP 未连接");
        }
        int id = Interlocked.Increment(ref _nextCommandId);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", id);
            writer.WriteString("method", method);
            if (parameters is { } p && p.ValueKind == JsonValueKind.Object)
            {
                writer.WritePropertyName("params");
                p.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
    }


    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (_ws?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new WebSocketException("对端关闭连接");
                    }
                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                HandleMessage(Encoding.UTF8.GetString(message.ToArray()));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Log?.Invoke($"CDP 连接断开：{e.Message}");
        }
        finally
        {
            Disconnected?.Invoke();
        }
    }


    private void HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("method", out var methodEl))
            {
                string method = methodEl.GetString() ?? "";
                var paramsEl = root.TryGetProperty("params", out var p) ? p : default;
                switch (method)
                {
                    case "Network.requestWillBeSent":
                        OnRequestWillBeSent(paramsEl);
                        break;
                    case "Network.loadingFinished":
                        OnLoadingFinished(paramsEl);
                        break;
                    case "Network.loadingFailed":
                        OnLoadingFailed(paramsEl);
                        break;
                }
            }
            else if (root.TryGetProperty("id", out _) && root.TryGetProperty("result", out var resultEl)
                && resultEl.TryGetProperty("body", out var bodyEl))
            {
                OnResponseBody(bodyEl.GetString());
            }
        }
        catch (Exception e)
        {
            Log?.Invoke($"解析 CDP 消息失败：{e.Message}");
        }
    }


    private void OnRequestWillBeSent(JsonElement paramsEl)
    {
        if (paramsEl.ValueKind != JsonValueKind.Object
            || !paramsEl.TryGetProperty("request", out var requestEl)
            || !requestEl.TryGetProperty("url", out var urlEl))
        {
            return;
        }
        string url = urlEl.GetString() ?? "";
        if (!url.Contains(TargetHost, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        bool hasDid = false, hasToken = false;
        if (requestEl.TryGetProperty("headers", out var headersEl) && headersEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in headersEl.EnumerateObject())
            {
                if (string.Equals(header.Name, "did", StringComparison.OrdinalIgnoreCase) && header.Value.ValueKind == JsonValueKind.String)
                {
                    hasDid = true;
                    string did = header.Value.GetString() ?? "";
                    if (did.Length > 0)
                    {
                        DidCaptured?.Invoke(did);
                        Log?.Invoke($"捕获 did：{did}");
                    }
                }
                else if (string.Equals(header.Name, "token", StringComparison.OrdinalIgnoreCase) && header.Value.ValueKind == JsonValueKind.String)
                {
                    hasToken = true;
                    string token = header.Value.GetString() ?? "";
                    if (token.Length > 0)
                    {
                        TokenCaptured?.Invoke(token);
                        Log?.Invoke("捕获 token");
                    }
                }
            }
        }

        if (hasDid && hasToken)
        {
            Log?.Invoke("已同时捕获 did 和 token");
        }

        
        if (paramsEl.TryGetProperty("requestId", out var idEl))
        {
            lock (_gate)
            {
                _trackedRequests[idEl.GetString() ?? ""] = url;
            }
            Log?.Invoke($"捕获库街区请求：{url}");
        }
    }


    private async void OnLoadingFinished(JsonElement paramsEl)
    {
        string? requestId = paramsEl.TryGetProperty("requestId", out var idEl) ? idEl.GetString() : null;
        if (requestId is null) return;
        bool tracked;
        lock (_gate)
        {
            tracked = _trackedRequests.Remove(requestId);
        }
        if (!tracked) return;
        try
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", Interlocked.Increment(ref _nextCommandId));
                writer.WriteString("method", "Network.getResponseBody");
                writer.WriteStartObject("params");
                writer.WriteString("requestId", requestId);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            var ws = _ws;
            if (ws?.State == WebSocketState.Open)
            {
                var bytes = stream.ToArray();
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
            }
        }
        catch (Exception e)
        {
            Log?.Invoke($"读取响应体失败：{e.Message}");
        }
    }


    private void OnLoadingFailed(JsonElement paramsEl)
    {
        if (paramsEl.TryGetProperty("requestId", out var idEl))
        {
            lock (_gate)
            {
                _trackedRequests.Remove(idEl.GetString() ?? "");
            }
        }
    }


    private void OnResponseBody(string? body)
    {
        if (string.IsNullOrEmpty(body) || !body.StartsWith('{')) return;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object
                && dataEl.TryGetProperty("userId", out var userIdEl))
            {
                string userId = userIdEl.ToString();
                if (userId.Length > 0 && userId != "0")
                {
                    UserIdCaptured?.Invoke(userId);
                    Log?.Invoke($"捕获 userId：{userId}");
                }
            }
        }
        catch { }
    }


    private async Task CloseQuietlyAsync()
    {
        var ws = _ws;
        _ws = null;
        if (ws is null) return;
        try
        {
            if (ws.State == WebSocketState.Open)
            {
                using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, closeCts.Token);
            }
        }
        catch { }
        ws.Dispose();
    }


    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await CloseQuietlyAsync();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
