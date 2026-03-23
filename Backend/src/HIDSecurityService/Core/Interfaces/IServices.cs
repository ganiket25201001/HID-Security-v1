namespace HIDSecurityService.Core.Interfaces;

/// <summary>
/// Service for monitoring USB device connections and disconnections.
/// </summary>
public interface IDeviceMonitorService : IDisposable
{
    /// <summary>
    /// Starts the device monitoring service.
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the device monitoring service.
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Gets all currently connected devices.
    /// </summary>
    IReadOnlyList<Models.UsbDevice> GetConnectedDevices();
    
    /// <summary>
    /// Gets a specific device by ID.
    /// </summary>
    Models.UsbDevice? GetDeviceById(string deviceId);
    
    /// <summary>
    /// Event raised when a device is connected.
    /// </summary>
    event EventHandler<Models.UsbDevice>? DeviceConnected;
    
    /// <summary>
    /// Event raised when a device is disconnected.
    /// </summary>
    event EventHandler<string>? DeviceDisconnected;
    
    /// <summary>
    /// Whether the monitor is currently running.
    /// </summary>
    bool IsRunning { get; }
}

/// <summary>
/// Service for tracking device state and history.
/// </summary>
public interface IDeviceTrackerService
{
    /// <summary>
    /// Registers a newly connected device.
    /// </summary>
    Task<Models.UsbDevice> RegisterDeviceAsync(Models.UsbDevice device);
    
    /// <summary>
    /// Updates device status.
    /// </summary>
    Task UpdateDeviceStatusAsync(string deviceId, Models.DeviceStatus status);
    
    /// <summary>
    /// Records device disconnection.
    /// </summary>
    Task RecordDisconnectionAsync(string deviceId);
    
    /// <summary>
    /// Gets device by fingerprint.
    /// </summary>
    Task<Models.UsbDevice?> GetDeviceByFingerprintAsync(string fingerprint);
    
    /// <summary>
    /// Gets all tracked devices.
    /// </summary>
    Task<IReadOnlyList<Models.UsbDevice>> GetAllDevicesAsync();
    
    /// <summary>
    /// Gets device connection history.
    /// </summary>
    Task<IReadOnlyList<DeviceConnectionRecord>> GetConnectionHistoryAsync(string deviceId, int limit = 100);
    
    /// <summary>
    /// Clears disconnected devices older than the specified date.
    /// </summary>
    Task<int> CleanupOldDevicesAsync(DateTime olderThan);
}

/// <summary>
/// Record of a device connection event.
/// </summary>
public sealed class DeviceConnectionRecord
{
    public string RecordId { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public TimeSpan? Duration => DisconnectedAt.HasValue ? DisconnectedAt.Value - ConnectedAt : null;
    public string? UserName { get; set; }
    public int SessionId { get; set; }
    public string? ComputerName { get; set; }
}

/// <summary>
/// Service for logging security events.
/// </summary>
public interface ISecurityEventLogger
{
    /// <summary>
    /// Logs a security event.
    /// </summary>
    Task LogEventAsync(Events.SecurityEvent securityEvent);
    
    /// <summary>
    /// Logs a security event to Windows Event Log.
    /// </summary>
    void LogToEventLog(Events.SecurityEvent securityEvent);
    
    /// <summary>
    /// Logs a security event to file.
    /// </summary>
    Task LogToFileAsync(Events.SecurityEvent securityEvent);
    
    /// <summary>
    /// Gets recent events.
    /// </summary>
    Task<IReadOnlyList<Events.SecurityEvent>> GetRecentEventsAsync(int count = 100);
    
    /// <summary>
    /// Exports events to a file.
    /// </summary>
    Task ExportEventsAsync(string filePath, DateTime? from = null, DateTime? to = null);
}

/// <summary>
/// Service for evaluating device policies.
/// </summary>
public interface IPolicyEvaluationService
{
    /// <summary>
    /// Evaluates a device against current policies.
    /// </summary>
    Task<Events.PolicyDecision> EvaluateDeviceAsync(Models.UsbDevice device);
    
    /// <summary>
    /// Evaluates a device action against current policies.
    /// </summary>
    Task<Events.PolicyDecision> EvaluateActionAsync(string deviceId, string action);
    
    /// <summary>
    /// Gets the active policy.
    /// </summary>
    Task<PolicyDocument?> GetActivePolicyAsync();
    
    /// <summary>
    /// Reloads policies from storage.
    /// </summary>
    Task ReloadPoliciesAsync();
    
    /// <summary>
    /// Event raised when policy is updated.
    /// </summary>
    event EventHandler<PolicyDocument>? PolicyUpdated;
}

/// <summary>
/// Policy document structure.
/// </summary>
public sealed class PolicyDocument
{
    public string PolicyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<PolicyRule> Rules { get; set; } = new();
    public PolicySettings Settings { get; set; } = new();
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Individual policy rule.
/// </summary>
public sealed class PolicyRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    public RuleCondition Condition { get; set; } = new();
    public Events.EnforcementAction Action { get; set; }
}

/// <summary>
/// Rule condition definition.
/// </summary>
public sealed class RuleCondition
{
    public List<ushort>? AllowedVendorIds { get; set; }
    public List<ushort>? BlockedVendorIds { get; set; }
    public List<ushort>? AllowedProductIds { get; set; }
    public List<ushort>? BlockedProductIds { get; set; }
    public List<string>? AllowedSerialNumbers { get; set; }
    public List<string>? BlockedSerialNumbers { get; set; }
    public List<byte>? AllowedDeviceClasses { get; set; }
    public List<byte>? BlockedDeviceClasses { get; set; }
    public bool? RequireSerialNumber { get; set; }
    public bool? BlockUnknownDevices { get; set; }
    public TimeSpan? MaxConnectionDuration { get; set; }
    public List<string>? AllowedUsbVersions { get; set; }
}

/// <summary>
/// Policy settings.
/// </summary>
public sealed class PolicySettings
{
    public bool EnableLogging { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public bool AutoQuarantine { get; set; } = false;
    public int QuarantineTimeoutMinutes { get; set; } = 30;
    public bool EnableAiScoring { get; set; } = false;
    public double AiThreatThreshold { get; set; } = 0.7;
    public bool BlockOnUnknownThreat { get; set; } = true;
}

/// <summary>
/// Service for AI-based threat scoring.
/// </summary>
public interface IThreatScoringService
{
    /// <summary>
    /// Analyzes a device and returns a threat score.
    /// </summary>
    Task<ThreatAnalysisResult> AnalyzeDeviceAsync(Models.UsbDevice device);
    
    /// <summary>
    /// Analyzes device behavior patterns.
    /// </summary>
    Task<ThreatAnalysisResult> AnalyzeBehaviorAsync(string deviceId, IEnumerable<DeviceBehaviorEvent> events);
    
    /// <summary>
    /// Gets the current model version.
    /// </summary>
    string ModelVersion { get; }
    
    /// <summary>
    /// Whether the AI service is available.
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// Result of threat analysis.
/// </summary>
public sealed class ThreatAnalysisResult
{
    /// <summary>
    /// Threat score (0.0 to 1.0).
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Risk level classification.
    /// </summary>
    public Models.RiskLevel RiskLevel { get; set; }
    
    /// <summary>
    /// Detected threat indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();
    
    /// <summary>
    /// Analysis explanation.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Recommended actions.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Analysis timestamp.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device behavior event for AI analysis.
/// </summary>
public sealed class DeviceBehaviorEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public Dictionary<string, object?> Data { get; set; } = new();
}

/// <summary>
/// Service for secure IPC communication.
/// </summary>
public interface IIpcCommunicationService : IDisposable
{
    /// <summary>
    /// Starts the IPC server.
    /// </summary>
    Task StartServerAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the IPC server.
    /// </summary>
    Task StopServerAsync();
    
    /// <summary>
    /// Sends a message to connected clients.
    /// </summary>
    Task BroadcastAsync(IpcMessage message);
    
    /// <summary>
    /// Whether the server is running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Number of connected clients.
    /// </summary>
    int ConnectedClientCount { get; }
    
    /// <summary>
    /// Event raised when a message is received.
    /// </summary>
    event EventHandler<IpcMessage>? MessageReceived;
}

/// <summary>
/// IPC message structure.
/// </summary>
public sealed class IpcMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MessageType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? SenderId { get; set; }
    public Dictionary<string, object?> Payload { get; set; } = new();
    public bool IsRequest { get; set; }
    public bool IsResponse { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Service configuration manager.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    ServiceConfiguration GetConfiguration();
    
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    T GetValue<T>(string key, T defaultValue);
    
    /// <summary>
    /// Gets a decrypted configuration value.
    /// </summary>
    T GetDecryptedValue<T>(string key, T defaultValue);
    
    /// <summary>
    /// Updates configuration.
    /// </summary>
    Task UpdateConfigurationAsync(Action<ServiceConfiguration> update);
    
    /// <summary>
    /// Validates configuration integrity.
    /// </summary>
    ConfigurationValidationResult ValidateConfiguration();
}

/// <summary>
/// Service configuration.
/// </summary>
public sealed class ServiceConfiguration
{
    public string ServiceName { get; set; } = "HIDSecurityService";
    public LoggingConfiguration Logging { get; set; } = new();
    public MonitoringConfiguration Monitoring { get; set; } = new();
    public PolicyConfiguration Policy { get; set; } = new();
    public IpcConfiguration Ipc { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
}

/// <summary>
/// Logging configuration.
/// </summary>
public sealed class LoggingConfiguration
{
    public string LogPath { get; set; } = "logs";
    public string EventLogSource { get; set; } = "HIDSecurityService";
    public string MinimumLogLevel { get; set; } = "Information";
    public int RetentionDays { get; set; } = 90;
    public long MaxFileSizeMb { get; set; } = 100;
    public bool EnableEventLog { get; set; } = true;
    public bool EnableFileLog { get; set; } = true;
}

/// <summary>
/// Monitoring configuration.
/// </summary>
public sealed class MonitoringConfiguration
{
    public int PollingIntervalMs { get; set; } = 1000;
    public bool EnableDeviceTracking { get; set; } = true;
    public bool EnableBehaviorAnalysis { get; set; } = true;
    public int MaxTrackedDevices { get; set; } = 1000;
    public TimeSpan DeviceHistoryRetention { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Policy configuration.
/// </summary>
public sealed class PolicyConfiguration
{
    public string PolicyPath { get; set; } = "policies";
    public string ActivePolicyId { get; set; } = "default";
    public bool AutoReloadOnchange { get; set; } = true;
    public int ReloadIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// IPC configuration.
/// </summary>
public sealed class IpcConfiguration
{
    public string PipeName { get; set; } = "HIDSecurityService";
    public bool EnableEncryption { get; set; } = true;
    public List<string> AllowedUsers { get; set; } = new();
    public List<string> AllowedGroups { get; set; } = new();
    public int MaxConnections { get; set; } = 10;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Security configuration.
/// </summary>
public sealed class SecurityConfiguration
{
    public bool EnableIntegrityChecks { get; set; } = true;
    public string IntegrityKeyPath { get; set; } = "security\\integrity.key";
    public bool EnableDpapiEncryption { get; set; } = true;
    public DataProtectionScope DpapiScope { get; set; } = DataProtectionScope.LocalMachine;
    public List<string> ProtectedPaths { get; set; } = new();
}

/// <summary>
/// Data protection scope for DPAPI.
/// </summary>
public enum DataProtectionScope
{
    CurrentUser = 0,
    LocalMachine = 1
}

/// <summary>
/// Configuration validation result.
/// </summary>
public sealed class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
