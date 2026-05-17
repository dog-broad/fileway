using System.Text.Json.Serialization;
using Fileway.Api.Configuration;
using Fileway.Api.Endpoints;
using Fileway.Api.Infrastructure;
using Fileway.Api.Jobs;
using Fileway.Api.Logging;
using Fileway.Shared.Detection;
using Fileway.Shared.Formats;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools.Definitions;
using Serilog;
using Serilog.Formatting.Compact;

// Bootstrap logger captures startup errors before full Serilog config is loaded
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Logging ---
    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    // --- Configuration ---
    var apiOptions = builder.Configuration
        .GetSection(ApiOptions.SectionName)
        .Get<ApiOptions>() ?? new ApiOptions();

    var rateLimitOptions = builder.Configuration
        .GetSection(RateLimitOptions.SectionName)
        .Get<RateLimitOptions>() ?? new RateLimitOptions();

    builder.Services.Configure<ApiOptions>(
        builder.Configuration.GetSection(ApiOptions.SectionName));
    builder.Services.Configure<RateLimitOptions>(
        builder.Configuration.GetSection(RateLimitOptions.SectionName));
    builder.Services.Configure<LibreOfficeOptions>(
        builder.Configuration.GetSection(LibreOfficeOptions.SectionName));
    builder.Services.Configure<StorageOptions>(
        builder.Configuration.GetSection(StorageOptions.SectionName));

    // --- Request size limit (Kestrel + form options) ---
    builder.WebHost.ConfigureKestrel(kestrel =>
        kestrel.Limits.MaxRequestBodySize = apiOptions.MaxRequestSizeBytes);

    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(form =>
        form.MultipartBodyLengthLimit = apiOptions.MaxRequestSizeBytes);

    // --- Infrastructure ---
    builder.Services.AddSingleton<ITierResolver, AlwaysFreeTierResolver>();

    builder.Services.AddSingleton<IFormatDetector>(_ => new FormatDetector(FileFormats.All));

    builder.Services.AddSingleton<IToolRegistry>(_ => new ToolRegistry([.. DataTools.All, .. ImageTools.All]));

    // --- Job model + storage ---
    builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
    builder.Services.AddSingleton<JobQueueManager>();
    builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<JobDispatcher>();
    builder.Services.AddHostedService<JobSweepService>();

    // --- Processors ---
    builder.Services.AddApiProcessors();
    builder.Services.AddHostedService<ProcessorSanityCheck>();

    // --- Logging ---
    builder.Services.AddSingleton<AuditLogService>();

    // --- Rate limiting ---
    builder.Services.AddFilewayRateLimiting(rateLimitOptions);

    // --- Health checks ---
    builder.Services.AddHealthChecks();

    // --- JSON serialization — enums as strings ---
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // --- ProblemDetails ---
    builder.Services.AddProblemDetails();

    // --- CORS ---
    builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                "https://fileway.io",
                "https://www.fileway.io",
                "http://localhost:5000",
                "https://localhost:5001",
                "http://localhost:5001",
                "https://localhost:7001",
                "http://localhost:5233",
                "https://localhost:7257",
                "http://127.0.0.1:5233",
                "https://127.0.0.1:7257")
            .AllowAnyHeader()
            .AllowAnyMethod()));

    // -----------------------------------------------------------------------

    var app = builder.Build();

    // Second-pass: populate ProcessorType on ToolDefinition records from the DI mapping.
    // Must run before ProcessorSanityCheck (which is an IHostedService that starts with app.Run()).
    app.InitializeProcessorTypes();

    // --- Middleware pipeline (order is required by spec) ---

    // 1. Exception handler wraps everything — must be first
    app.UseExceptionHandler(ProblemDetailsExceptionHandler.Configure);

    // 2. HTTPS redirection
    app.UseHttpsRedirection();

    // 3. Security headers
    app.UseSecurityHeaders();

    // 4. CORS
    app.UseCors();

    // 5. Session token (validates token, computes ipHash for rate limiter)
    app.UseSessionToken();

    // 6. Rate limiting
    app.UseRateLimiter();

    // 7. Serilog request logging (after rate limiter so throttled requests are logged)
    app.UseSerilogRequestLogging();

    // --- Endpoints ---
    app.MapHealthChecks("/health/live").DisableRateLimiting();
    app.MapHealthChecks("/health/ready").DisableRateLimiting();
    app.MapToolEndpoints();
    app.MapDetectEndpoints();
    app.MapJobEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Required by WebApplicationFactory<Program> in integration tests
public partial class Program { }
