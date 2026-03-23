using HIDSecurityService.Configuration;
using HIDSecurityService.Core.Interfaces;
using HIDSecurityService.Services.AI;
using HIDSecurityService.Services.DeviceMonitoring;
using HIDSecurityService.Services.DeviceTracking;
using HIDSecurityService.Services.IPC;
using HIDSecurityService.Services.Logging;
using HIDSecurityService.Services.Policy;
using Microsoft.Extensions.Options;
using Serilog;

namespace HIDSecurityService;

/// <summary>
/// Main Windows Service implementation.
/// </summary>
public class HidSecurityWindowsService : BackgroundService
{
    private readonly ILogger<HidSecurityWindowsService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IDeviceMonitorService _deviceMonitor;
    private readonly IDeviceTrackerService _deviceTracker;
    private readonly ISecurityEventLogger _eventLogger;
    private readonly IPolicyEvaluationService _policyService;
    private readonly IThreatScoringService _threatScoring;
    private readonly IIpcCommunicationService _ipcService;
    private readonly Configuration.ConfigurationManager _configManager;
    private readonly ServiceConfiguration _config;

    public HidSecurityWindowsService(
        ILogger<HidSecurityWindowsService> logger,
        IHostApplicationLifetime lifetime,
        IDeviceMonitorService deviceMonitor,
        IDeviceTrackerService deviceTracker,
        ISecurityEventLogger eventLogger,
        IPolicyEvaluationService policyService,
        IThreatScoringService threatScoring,
        IIpcCommunicationService ipcService,
        Configuration.ConfigurationManager configManager,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _deviceMonitor = deviceMonitor ?? throw new ArgumentNullException(nameof(deviceMonitor));
        _deviceTracker = deviceTracker ?? throw new ArgumentNullException(nameof(deviceTracker));
        _eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
        _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
        _threatScoring = threatScoring ?? throw new ArgumentNullException(nameof(threatScoring));
        _ipcService = ipcService ?? throw new ArgumentNullException(nameof(ipcService));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _config = config.Value;
    }

    /// <summary>
    /// Service startup.
    /// </summary>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===========================================");
        _logger.LogInformation("HID Security Service Starting");
        _logger.LogInformation("Version: {Version}", typeof(HidSecurityWindowsService).Assembly.GetName().Version);
        _logger.LogInformation("Machine: {MachineName}", Environment.MachineName);
        _logger.LogInformation("OS Version: {OSVersion}", Environment.OSVersion);
        _logger.LogInformation("===========================================");

        // Validate configuration
        var validationResult = _configManager.ValidateConfiguration();
        if (!validationResult.IsValid)
        {
            _logger.LogError("Configuration validation failed: {Errors}", 
                string.Join("; ", validationResult.Errors));
        }

        foreach (var warning in validationResult.Warnings)
        {
            _logger.LogWarning("Configuration warning: {Warning}", warning);
        }

        // Load persisted devices
        if (_deviceTracker is DeviceTrackerService tracker)
        {
            await tracker.LoadPersistedDevicesAsync();
        }

        // Log service start event
        await _eventLogger.LogEventAsync(new Core.Events.ServiceLifecycleEvent
        {
            EventType = "ServiceStarted",
            Title = "HID Security Service Started",
            Description = $"Service started on {Environment.MachineName}",
            Source = "HidSecurityWindowsService"
        });

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Service shutdown.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HID Security Service Stopping");

        try
        {
            // Log service stop event
            await _eventLogger.LogEventAsync(new Core.Events.ServiceLifecycleEvent
            {
                EventType = "ServiceStopped",
                Title = "HID Security Service Stopped",
                Description = "Service stopped gracefully",
                Source = "HidSecurityWindowsService"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging service stop event");
        }

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("HID Security Service Stopped");
        Log.Information("===========================================");
    }

    /// <summary>
    /// Main service execution loop.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Start IPC server
            await _ipcService.StartServerAsync(stoppingToken);
            _logger.LogInformation("IPC communication service started");

            // Start device monitoring
            await _deviceMonitor.StartMonitoringAsync(stoppingToken);
            _logger.LogInformation("Device monitoring service started");

            // Subscribe to device events
            _deviceMonitor.DeviceConnected += OnDeviceConnectedAsync;
            _deviceMonitor.DeviceDisconnected += OnDeviceDisconnectedAsync;

            _logger.LogInformation("HID Security Service is now running");

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service error occurred");
            throw;
        }
        finally
        {
            // Cleanup
            _deviceMonitor.DeviceConnected -= OnDeviceConnectedAsync;
            _deviceMonitor.DeviceDisconnected -= OnDeviceDisconnectedAsync;

            await _deviceMonitor.StopMonitoringAsync();
            await _ipcService.StopServerAsync();
        }
    }

    /// <summary>
    /// Handles device connected event.
    /// </summary>
    private async void OnDeviceConnectedAsync(object? sender, Core.Models.UsbDevice device)
    {
        try
        {
            // Evaluate policy
            var policyDecision = await _policyService.EvaluateDeviceAsync(device);
            
            // Get threat score if available
            ThreatAnalysisResult? threatResult = null;
            if (_threatScoring.IsAvailable)
            {
                threatResult = await _threatScoring.AnalyzeDeviceAsync(device);
            }

            // Log the event
            var securityEvent = new Core.Events.DeviceConnectedEvent
            {
                Title = $"Device Connected: {device.DeviceName}",
                Description = $"VID:{device.VendorId:X4} PID:{device.ProductId:X4}",
                Source = "DeviceMonitor",
                DeviceId = device.DeviceId,
                Severity = policyDecision.IsAllowed ? Core.Events.EventSeverity.Informational : Core.Events.EventSeverity.Warning,
                DeviceInfo = new Core.Events.UsbDeviceInfo
                {
                    DeviceId = device.DeviceId,
                    VendorId = device.VendorId,
                    ProductId = device.ProductId,
                    SerialNumber = device.SerialNumber,
                    DeviceName = device.DeviceName,
                    DeviceClass = device.DeviceClass,
                    Fingerprint = device.GetFingerprint()
                },
                IsKnownDevice = !string.IsNullOrEmpty(device.SerialNumber),
                InitialDecision = policyDecision,
                Data =
                {
                    ["PolicyAllowed"] = policyDecision.IsAllowed,
                    ["PolicyReason"] = policyDecision.Reason,
                    ["ThreatScore"] = threatResult?.Score,
                    ["ThreatLevel"] = threatResult?.RiskLevel.ToString()
                }
            };

            await _eventLogger.LogEventAsync(securityEvent);

            // Broadcast to connected clients
            await _ipcService.BroadcastAsync(new Core.Interfaces.IpcMessage
            {
                MessageType = Services.IPC.IpcMessageTypes.DeviceListResponse,
                Payload = new Dictionary<string, object?>
                {
                    ["Action"] = "DeviceConnected",
                    ["DeviceId"] = device.DeviceId,
                    ["DeviceName"] = device.DeviceName,
                    ["IsAuthorized"] = device.IsAuthorized,
                    ["RiskLevel"] = device.RiskLevel.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device connection");
        }
    }

    /// <summary>
    /// Handles device disconnected event.
    /// </summary>
    private async void OnDeviceDisconnectedAsync(object? sender, string deviceId)
    {
        try
        {
            // Log the event
            var securityEvent = new Core.Events.DeviceDisconnectedEvent
            {
                Title = "Device Disconnected",
                Description = $"Device {deviceId} was disconnected",
                Source = "DeviceMonitor",
                DeviceId = deviceId,
                Severity = Core.Events.EventSeverity.Informational,
                Data = { ["DeviceId"] = deviceId }
            };

            await _eventLogger.LogEventAsync(securityEvent);

            // Broadcast to connected clients
            await _ipcService.BroadcastAsync(new Core.Interfaces.IpcMessage
            {
                MessageType = Services.IPC.IpcMessageTypes.DeviceListResponse,
                Payload = new Dictionary<string, object?>
                {
                    ["Action"] = "DeviceDisconnected",
                    ["DeviceId"] = deviceId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device disconnection");
        }
    }
}

/// <summary>
/// Service started event.
/// </summary>
public class ServiceStartedEvent : Core.Events.SecurityEvent
{
    public ServiceStartedEvent()
    {
        EventType = nameof(ServiceStartedEvent);
        Category = Core.Events.EventCategory.ServiceLifecycle;
        Severity = Core.Events.EventSeverity.Informational;
    }
}

/// <summary>
/// Service stopped event.
/// </summary>
public class ServiceStoppedEvent : Core.Events.SecurityEvent
{
    public ServiceStoppedEvent()
    {
        EventType = nameof(ServiceStoppedEvent);
        Category = Core.Events.EventCategory.ServiceLifecycle;
        Severity = Core.Events.EventSeverity.Informational;
    }
}
