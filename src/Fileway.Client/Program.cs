using Fileway.Shared.Detection;
using Fileway.Shared.Formats;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Fileway.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<IFormatDetector>(_ => new FormatDetector([
    FileFormats.Json, FileFormats.Yaml, FileFormats.Csv, FileFormats.Toml,
    FileFormats.Xlsx, FileFormats.Txt, FileFormats.Md
]));

await builder.Build().RunAsync();
