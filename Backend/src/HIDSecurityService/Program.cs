using Serilog;
using HIDSecurityService;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "bootstrap-.log"),
        rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("===========================================");
    Log.Information("HID Security Service Initializing");
    Log.Information("===========================================");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "HIDSecurityService";
    });

    // Load configuration
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    // Register services
    builder.Services.AddHostedService<HidSecurityWindowsService>();

    // Use Serilog
    builder.Services.AddSerilog((services, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "service-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90);
        
        if (OperatingSystem.IsWindows())
        {
            loggerConfig.WriteTo.EventLog("HIDSecurityService", manageEventSource: true);
        }
    });

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("===========================================");
    Log.CloseAndFlush();
}
