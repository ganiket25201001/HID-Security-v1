using HIDSecurityService;
using HIDSecurityService.Configuration;
using HIDSecurityService.Core.Interfaces;
using HIDSecurityService.Services.AI;
using HIDSecurityService.Services.DeviceMonitoring;
using HIDSecurityService.Services.DeviceTracking;
using HIDSecurityService.Services.IPC;
using HIDSecurityService.Services.Logging;
using HIDSecurityService.Services.Policy;
using Serilog;

// Configure Serilog early for bootstrap logging
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

    // Configure hosting for Windows Service
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "HIDSecurityService";
    });

    // Load configuration
    builder.Configuration.AddJsonFile(
        "appsettings.json",
        optional: false,
        reloadOnChange: true);

    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true);

    // Bind configuration
    builder.Services.Configure<ServiceConfiguration>(
        builder.Configuration.GetSection("ServiceConfiguration"));

    // Add Serilog
    builder.Services.AddSerilog((services, loggerConfig) =>
    {
        var config = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ServiceConfiguration>>();
        
        loggerConfig
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning);

        // File sink
        var logPath = Path.Combine(AppContext.BaseDirectory, config.Value.Logging.LogPath);
        Directory.CreateDirectory(logPath);
        
        loggerConfig.WriteTo.File(
            Path.Combine(logPath, "service-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: config.Value.Logging.RetentionDays,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // Event Log sink (Windows only)
        if (OperatingSystem.IsWindows() && config.Value.Logging.EnableEventLog)
        {
            loggerConfig.WriteTo.EventLog(
                config.Value.Logging.EventLogSource,
                manageEventSource: true);
        }
    });

    // Register services
    builder.Services.AddSingleton<IConfigurationManager, ConfigurationManager>();
    builder.Services.AddSingleton<IDeviceMonitorService, WinApiDeviceMonitorService>();
    builder.Services.AddSingleton<IDeviceTrackerService, DeviceTrackerService>();
    builder.Services.AddSingleton<ISecurityEventLogger, SecurityEventLoggerService>();
    builder.Services.AddSingleton<IPolicyEvaluationService, PolicyEvaluationService>();
    builder.Services.AddSingleton<IThreatScoringService, ThreatScoringService>();
    builder.Services.AddSingleton<IIpcCommunicationService, NamedPipeCommunicationService>();

    // Register the Windows Service
    builder.Services.AddSingleton<HidSecurityWindowsService>();
    builder.Services.AddHostedService(provider =>
        provider.GetRequiredService<HidSecurityWindowsService>());

    var app = builder.Build();

    // Use Serilog
    app.Services.UseSerilog();

    Log.Information("Starting HID Security Service");

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
