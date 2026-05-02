using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Fileway.Shared.Jobs;
using Microsoft.JSInterop;

namespace Fileway.Client.Interop;

public sealed class SseClient : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<SseClient>? _selfRef;
    private string? _connectionId;
    private readonly Channel<JobEvent> _channel = Channel.CreateUnbounded<JobEvent>();

    public SseClient(IJSRuntime js)
    {
        _js = js;
    }

    public async Task OpenAsync(string url, CancellationToken ct)
    {
        _selfRef = DotNetObjectReference.Create(this);
        _connectionId = await _js.InvokeAsync<string>("SseClient.open", ct, url, _selfRef);
    }

    [JSInvokable]
    public void OnMessage(string data, string eventId)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(data);
            var typeStr = element.GetProperty("type").GetString() ?? string.Empty;
            var eventType = Enum.TryParse<JobEventType>(typeStr, ignoreCase: true, out var parsed)
                ? parsed
                : JobEventType.Failed;

            var jobEvent = new JobEvent
            {
                EventType = eventType,
                EventId = eventId,
                Payload = element.TryGetProperty("payload", out var payload) ? payload : default
            };

            _channel.Writer.TryWrite(jobEvent);

            if (jobEvent.EventType is JobEventType.Completed or JobEventType.Failed)
                _channel.Writer.TryComplete();
        }
        catch
        {
            _channel.Writer.TryComplete();
        }
    }

    [JSInvokable]
    public void OnError()
    {
        _channel.Writer.TryComplete(new IOException("SSE connection error"));
    }

    public async IAsyncEnumerable<JobEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var e in _channel.Reader.ReadAllAsync(ct))
            yield return e;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionId is not null)
        {
            try { await _js.InvokeVoidAsync("SseClient.close", _connectionId); }
            catch { /* best effort */ }
        }
        _channel.Writer.TryComplete();
        _selfRef?.Dispose();
    }
}
