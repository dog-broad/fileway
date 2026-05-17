using Microsoft.JSInterop;

namespace Fileway.Client.Services;

public sealed class SessionTokenProvider
{
    private string? _token;

    public string Token => _token ?? throw new InvalidOperationException("Session token not initialised.");

    public async Task InitialiseAsync(IJSRuntime js)
    {
        var existing = await js.InvokeAsync<string?>("sessionStorage.getItem", "sessionToken");
        if (string.IsNullOrWhiteSpace(existing))
        {
            var token = Guid.NewGuid().ToString("D");
            await js.InvokeVoidAsync("sessionStorage.setItem", "sessionToken", token);
            _token = token;
        }
        else
        {
            _token = existing;
        }
    }
}
