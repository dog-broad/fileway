using Microsoft.JSInterop;

namespace Fileway.Client.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime _js;
    private string _currentTheme = "dark";

    public string CurrentTheme => _currentTheme;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitialiseAsync()
    {
        _currentTheme = await _js.InvokeAsync<string>("ThemeInterop.getTheme");
    }

    public async Task ToggleAsync()
    {
        _currentTheme = _currentTheme == "dark" ? "light" : "dark";
        await _js.InvokeVoidAsync("ThemeInterop.setTheme", _currentTheme);
    }

    public async Task SetThemeAsync(string theme)
    {
        _currentTheme = theme;
        await _js.InvokeVoidAsync("ThemeInterop.setTheme", theme);
    }
}
