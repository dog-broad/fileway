using Fileway.Client.Infrastructure;
using Fileway.Client.Services;
using Fileway.Shared.Detection;
using Fileway.Shared.Formats;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools.Definitions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fileway.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- HTTP client pointing at the API ---
// In development appsettings.Development.json sets ApiBaseUrl to the API's port.
// In production the API and client are served from the same origin so HostEnvironment.BaseAddress is correct.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// --- Shared singletons ---
builder.Services.AddSingleton<IFormatDetector>(_ => new FormatDetector([
    FileFormats.Json, FileFormats.Yaml, FileFormats.Csv, FileFormats.Toml,
    FileFormats.Xlsx, FileFormats.Txt, FileFormats.Md
]));

builder.Services.AddSingleton<IToolRegistry>(_ => new ToolRegistry(DataTools.All));

// --- Session token (singleton — one token per tab lifetime) ---
builder.Services.AddSingleton<SessionTokenProvider>();

// --- Client services (scoped — one per tab) ---
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<DetectionService>();
builder.Services.AddScoped<ApiJobClient>();
builder.Services.AddScoped<ProcessorRouter>();
builder.Services.AddScoped<ToolStateService>();
builder.Services.AddScoped<Fileway.Client.Interop.SseClient>();

// --- WASM processors ---
builder.Services.AddWasmProcessors();

var host = builder.Build();

// Initialise session token from sessionStorage before any component renders
var tokenProvider = host.Services.GetRequiredService<SessionTokenProvider>();
var js = host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
await tokenProvider.InitialiseAsync(js);

await host.RunAsync();
