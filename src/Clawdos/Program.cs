using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using Clawdos.Configuration;
using Clawdos.Endpoints;
using Clawdos.Middleware;
using Clawdos.Services;
using Microsoft.Extensions.Hosting.WindowsServices;

// ── Configure Host ──────────────────────────────────────────────
var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};

var builder = WebApplication.CreateBuilder(options);
builder.Host.UseWindowsService();           // support sc create to register as Windows Service

// ── Load clawdos-config.json ───────────────────────────────
var configPath = Path.Combine(AppContext.BaseDirectory, "Clawdos/clawdos-config.json");
if (!File.Exists(configPath))
    throw new FileNotFoundException($"Configuration file not found: {configPath}");

var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var config = JsonSerializer.Deserialize<ClawdosConfig>(File.ReadAllText(configPath), jsonOpts)
    ?? throw new InvalidOperationException("Failed to deserialize clawdos-config.json");

// ── Environment variable overrides ──────────────────────────────────────────
config.ListenIp  = Environment.GetEnvironmentVariable("CLAWDOS_LISTEN_IP") ?? config.ListenIp;
config.Port      = int.TryParse(Environment.GetEnvironmentVariable("CLAWDOS_PORT"), out var envPort) ? envPort : config.Port;
config.ApiKey    = Environment.GetEnvironmentVariable("CLAWDOS_API_KEY") ?? config.ApiKey;

builder.Services.AddSingleton(config);

// ── Kestrel listening configuration ──────────────────────────────────────
builder.WebHost.ConfigureKestrel(k =>
{
    k.Listen(IPAddress.Parse(config.ListenIp), config.Port);
});

// ── Register services ──────────────────────────────────────────────
builder.Services.AddSingleton<HealthMetricsService>();
builder.Services.AddSingleton<EnvironmentService>();
builder.Services.AddSingleton<ScreenCaptureService>();
builder.Services.AddSingleton<InputInjectionService>();
builder.Services.AddSingleton<WindowManagementService>();
builder.Services.AddSingleton<FileSandboxService>();
builder.Services.AddSingleton<ShellService>();

var app = builder.Build();

// ── Middleware pipeline (order sensitive) ────────────────────────────
app.UseMiddleware<MetricsMiddleware>();      // record metrics for all requests, including auth failures
app.UseMiddleware<ApiKeyAuthMiddleware>();   // authenticate requests before they reach the endpoints

// ── Map routes ──────────────────────────────────────────────
app.MapHealthEndpoints();
app.MapScreenEndpoints();
app.MapInputEndpoints();
app.MapWindowEndpoints();
app.MapFileSystemEndpoints();
app.MapShellEndpoints();

// ── Startup ──────────────────────────────────────────────────
app.Logger.LogInformation(
    "Clawdos starting on {Ip}:{Port}  (clientId={ClientId})",
    config.ListenIp, config.Port, config.ClientId);

if (WindowsServiceHelpers.IsWindowsService() || args.Contains("--console"))
{
    app.Run();
}
else
{
    app.DisposeAsync().GetAwaiter().GetResult(); // Dispose the initial app
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new Clawdos.TrayApplicationContext(
        () => 
        {
            var newBuilder = WebApplication.CreateBuilder(options);
            newBuilder.Host.UseWindowsService();
            newBuilder.Services.AddSingleton(config);
            newBuilder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Parse(config.ListenIp), config.Port));
            
            newBuilder.Services.AddSingleton<HealthMetricsService>();
            newBuilder.Services.AddSingleton<EnvironmentService>();
            newBuilder.Services.AddSingleton<ScreenCaptureService>();
            newBuilder.Services.AddSingleton<InputInjectionService>();
            newBuilder.Services.AddSingleton<WindowManagementService>();
            newBuilder.Services.AddSingleton<FileSandboxService>();
            newBuilder.Services.AddSingleton<ShellService>();
            
            var newApp = newBuilder.Build();
            newApp.UseMiddleware<MetricsMiddleware>();
            newApp.UseMiddleware<ApiKeyAuthMiddleware>();
            
            newApp.MapHealthEndpoints();
            newApp.MapScreenEndpoints();
            newApp.MapInputEndpoints();
            newApp.MapWindowEndpoints();
            newApp.MapFileSystemEndpoints();
            newApp.MapShellEndpoints();
            return newApp;
        }));
}

