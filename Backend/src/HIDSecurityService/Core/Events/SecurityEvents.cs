namespace HIDSecurityService.Core.Events;

/// <summary>
/// Base class for all security events.
/// </summary>
public abstract class SecurityEvent
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Event timestamp in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Event type name.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Event severity level.
    /// </summary>
    public EventSeverity Severity { get; set; }
    
    /// <summary>
    /// Event category.
    /// </summary>
    public EventCategory Category { get; set; }
    
    /// <summary>
    /// Event title/summary.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed event description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Source component that generated the event.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Computer name where the event occurred.
    /// </summary>
    public string ComputerName { get; set; } = Environment.MachineName;
    
    /// <summary>
    /// Additional event data.
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();
    
    /// <summary>
    /// Related device ID, if applicable.
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Related user SID, if applicable.
    /// </summary>
    public string? UserSid { get; set; }
    
    /// <summary>
    /// Related session ID, if applicable.
    /// </summary>
    public int? SessionId { get; set; }
    
    /// <summary>
    /// Whether this event has been processed.
    /// </summary>
    public bool IsProcessed { get; set; }
    
    /// <summary>
    /// Processing result or notes.
    /// </summary>
    public string? ProcessingResult { get; set; }
}

/// <summary>
/// Event severity levels.
/// </summary>
public enum EventSeverity
{
    /// <summary>
    /// Informational event.
    /// </summary>
    Informational = 0,
    
    /// <summary>
    /// Warning event.
    /// </summary>
    Warning = 1,
    
    /// <summary>
    /// Error event.
    /// </summary>
    Error = 2,
    
    /// <summary>
    /// Critical event requiring immediate attention.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Event categories for classification.
/// </summary>
public enum EventCategory
{
    /// <summary>
    /// Device connection events.
    /// </summary>
    DeviceConnection = 0,
    
    /// <summary>
    /// Policy evaluation events.
    /// </summary>
    PolicyEvaluation = 1,
    
    /// <summary>
    /// Security threat events.
    /// </summary>
    SecurityThreat = 2,
    
    /// <summary>
    /// Service lifecycle events.
    /// </summary>
    ServiceLifecycle = 3,
    
    /// <summary>
    /// Configuration events.
    /// </summary>
    Configuration = 4,
    
    /// <summary>
    /// IPC communication events.
    /// </summary>
    IpcCommunication = 5,
    
    /// <summary>
    /// AI/ML analysis events.
    /// </summary>
    AiAnalysis = 6,
    
    /// <summary>
    /// General system events.
    /// </summary>
    System = 7
}

/// <summary>
/// Event raised when a device is connected.
/// </summary>
public sealed class DeviceConnectedEvent : SecurityEvent
{
    public DeviceConnectedEvent()
    {
        EventType = nameof(DeviceConnectedEvent);
        Category = EventCategory.DeviceConnection;
    }
    
    /// <summary>
    /// Connected device information.
    /// </summary>
    public UsbDeviceInfo DeviceInfo { get; set; } = new();
    
    /// <summary>
    /// Whether this is a known device.
    /// </summary>
    public bool IsKnownDevice { get; set; }
    
    /// <summary>
    /// Initial policy decision.
    /// </summary>
    public PolicyDecision InitialDecision { get; set; }
}

/// <summary>
/// Event raised when a device is disconnected.
/// </summary>
public sealed class DeviceDisconnectedEvent : SecurityEvent
{
    public DeviceDisconnectedEvent()
    {
        EventType = nameof(DeviceDisconnectedEvent);
        Category = EventCategory.DeviceConnection;
    }
    
    /// <summary>
    /// Disconnected device ID.
    /// </summary>
    public new string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device fingerprint.
    /// </summary>
    public string? Fingerprint { get; set; }
    
    /// <summary>
    /// Connection duration.
    /// </summary>
    public TimeSpan ConnectionDuration { get; set; }
}

/// <summary>
/// Event raised when a policy violation is detected.
/// </summary>
public sealed class PolicyViolationEvent : SecurityEvent
{
    public PolicyViolationEvent()
    {
        EventType = nameof(PolicyViolationEvent);
        Category = EventCategory.PolicyEvaluation;
        Severity = EventSeverity.Warning;
    }
    
    /// <summary>
    /// Violated policy ID.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Violated rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;
    
    /// <summary>
    /// Violation description.
    /// </summary>
    public string ViolationDetails { get; set; } = string.Empty;
    
    /// <summary>
    /// Enforcement action taken.
    /// </summary>
    public EnforcementAction ActionTaken { get; set; }
}

/// <summary>
/// Event raised when a security threat is detected.
/// </summary>
public sealed class SecurityThreatEvent : SecurityEvent
{
    public SecurityThreatEvent()
    {
        EventType = nameof(SecurityThreatEvent);
        Category = EventCategory.SecurityThreat;
        Severity = EventSeverity.Critical;
    }
    
    /// <summary>
    /// Threat type.
    /// </summary>
    public ThreatType ThreatType { get; set; }
    
    /// <summary>
    /// Threat indicators.
    /// </summary>
    public List<string> Indicators { get; set; } = new();
    
    /// <summary>
    /// Recommended response.
    /// </summary>
    public string RecommendedResponse { get; set; } = string.Empty;
    
    /// <summary>
    /// AI confidence score, if applicable.
    /// </summary>
    public double? ConfidenceScore { get; set; }
}

/// <summary>
/// USB device information for events.
/// </summary>
public sealed class UsbDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public ushort VendorId { get; set; }
    public ushort ProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public byte DeviceClass { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
}

/// <summary>
/// Policy decision result.
/// </summary>
public sealed class PolicyDecision
{
    /// <summary>
    /// Whether the action is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }
    
    /// <summary>
    /// Decision reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Applied policy ID.
    /// </summary>
    public string? PolicyId { get; set; }
    
    /// <summary>
    /// Applied rule IDs.
    /// </summary>
    public List<string> RuleIds { get; set; } = new();
    
    /// <summary>
    /// Enforcement action.
    /// </summary>
    public EnforcementAction EnforcementAction { get; set; }
}

/// <summary>
/// Enforcement actions that can be taken.
/// </summary>
public enum EnforcementAction
{
    /// <summary>
    /// No action taken.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Allow the device/action.
    /// </summary>
    Allow = 1,
    
    /// <summary>
    /// Block the device/action.
    /// </summary>
    Block = 2,
    
    /// <summary>
    /// Quarantine for analysis.
    /// </summary>
    Quarantine = 3,
    
    /// <summary>
    /// Notify only.
    /// </summary>
    Notify = 4,
    
    /// <summary>
    /// Log only.
    /// </summary>
    LogOnly = 5
}

/// <summary>
/// Security threat types.
/// </summary>
public enum ThreatType
{
    /// <summary>
    /// Unknown threat type.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// BadUSB/HID attack.
    /// </summary>
    BadUsbAttack = 1,
    
    /// <summary>
    /// USB rubber ducky attack.
    /// </summary>
    RubberDucky = 2,
    
    /// <summary>
    /// Mass storage malware.
    /// </summary>
    StorageMalware = 3,
    
    /// <summary>
    /// Device spoofing.
    /// </summary>
    DeviceSpoofing = 4,
    
    /// <summary>
    /// Unauthorized device.
    /// </summary>
    UnauthorizedDevice = 5,
    
    /// <summary>
    /// Suspicious behavior.
    /// </summary>
    SuspiciousBehavior = 6,
    
    /// <summary>
    /// Firmware tampering.
    /// </summary>
    FirmwareTampering = 7
}
