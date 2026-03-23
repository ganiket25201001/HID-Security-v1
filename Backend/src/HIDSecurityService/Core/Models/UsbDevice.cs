namespace HIDSecurityService.Core.Models;

/// <summary>
/// Represents a USB/HID device connected to the system.
/// </summary>
public sealed class UsbDevice
{
    /// <summary>
    /// Unique identifier for this device instance.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Vendor ID (VID) in hexadecimal format (e.g., "046D").
    /// </summary>
    public ushort VendorId { get; set; }
    
    /// <summary>
    /// Product ID (PID) in hexadecimal format (e.g., "C52B").
    /// </summary>
    public ushort ProductId { get; set; }
    
    /// <summary>
    /// Device serial number, if available.
    /// </summary>
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Human-readable device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Device manufacturer name.
    /// </summary>
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// USB device class code.
    /// </summary>
    public byte DeviceClass { get; set; }
    
    /// <summary>
    /// USB device subclass code.
    /// </summary>
    public byte DeviceSubClass { get; set; }
    
    /// <summary>
    /// USB device protocol code.
    /// </summary>
    public byte DeviceProtocol { get; set; }
    
    /// <summary>
    /// USB version number (e.g., 2.0, 3.0).
    /// </summary>
    public Version? UsbVersion { get; set; }
    
    /// <summary>
    /// Parent device ID (for composite devices).
    /// </summary>
    public string? ParentDeviceId { get; set; }
    
    /// <summary>
    /// Hub and port information.
    /// </summary>
    public string? HubPath { get; set; }
    
    /// <summary>
    /// Port number on the hub.
    /// </summary>
    public byte? PortNumber { get; set; }
    
    /// <summary>
    /// Time when the device was first connected.
    /// </summary>
    public DateTime FirstConnected { get; set; }
    
    /// <summary>
    /// Time of the last connection event.
    /// </summary>
    public DateTime LastConnected { get; set; }
    
    /// <summary>
    /// Time of the last disconnection event.
    /// </summary>
    public DateTime? LastDisconnected { get; set; }
    
    /// <summary>
    /// Current connection status.
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Whether the device is authorized by policy.
    /// </summary>
    public bool IsAuthorized { get; set; }
    
    /// <summary>
    /// Current device status.
    /// </summary>
    public DeviceStatus Status { get; set; }
    
    /// <summary>
    /// Risk level assigned to this device.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }
    
    /// <summary>
    /// List of device capabilities.
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
    
    /// <summary>
    /// Custom device properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// Creates a unique fingerprint for this device.
    /// </summary>
    public string GetFingerprint() => $"{VendorId:X4}:{ProductId:X4}:{SerialNumber ?? "NOSERIAL"}";
    
    /// <summary>
    /// Gets the VID:PID string representation.
    /// </summary>
    public string GetVidPid() => $"{VendorId:X4}:{ProductId:X4}";
}

/// <summary>
/// Current status of a USB device.
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// Device status is unknown.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Device is connected and authorized.
    /// </summary>
    Connected = 1,
    
    /// <summary>
    /// Device is connected but blocked by policy.
    /// </summary>
    Blocked = 2,
    
    /// <summary>
    /// Device is quarantined for analysis.
    /// </summary>
    Quarantined = 3,
    
    /// <summary>
    /// Device was disconnected.
    /// </summary>
    Disconnected = 4,
    
    /// <summary>
    /// Device is in a fault state.
    /// </summary>
    Fault = 5
}

/// <summary>
/// Risk level classification for devices.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// No risk identified.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Low risk - minor policy deviation.
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Medium risk - requires attention.
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// High risk - potential threat.
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Critical risk - immediate action required.
    /// </summary>
    Critical = 4
}

/// <summary>
/// USB device class codes (partial list).
/// </summary>
public static class UsbDeviceClasses
{
    public const byte Unspecified = 0x00;
    public const byte Audio = 0x01;
    public const byte Communications = 0x02;
    public const byte HumanInterfaceDevice = 0x03;
    public const byte PhysicalInterfaceDevice = 0x05;
    public const byte Image = 0x06;
    public const byte Printer = 0x07;
    public const byte MassStorage = 0x08;
    public const byte Hub = 0x09;
    public const byte CdcData = 0x0A;
    public const byte SmartCard = 0x0B;
    public const byte ContentSecurity = 0x0D;
    public const byte Video = 0x0E;
    public const byte PersonalHealthcare = 0x0F;
    public const byte AudioVideo = 0xEF;
    public const byte Diagnostic = 0xDC;
    public const byte Wireless = 0xE0;
    public const byte Miscellaneous = 0xEF;
    public const byte ApplicationSpecific = 0xFE;
    public const byte VendorSpecific = 0xFF;
}
