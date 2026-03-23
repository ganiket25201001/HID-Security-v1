using System.Runtime.InteropServices;
using HIDSecurityService.Core.Interfaces;
using HIDSecurityService.Core.Models;
using Microsoft.Extensions.Logging;

namespace HIDSecurityService.Services.DeviceMonitoring;

/// <summary>
/// Windows API-based USB device monitoring service.
/// Uses RegisterDeviceNotification for real-time device change detection.
/// </summary>
public sealed class WinApiDeviceMonitorService : IDeviceMonitorService
{
    private readonly ILogger<WinApiDeviceMonitorService> _logger;
    private readonly IDeviceTrackerService _deviceTracker;
    private readonly object _lock = new();
    private readonly Dictionary<string, UsbDevice> _connectedDevices = new();
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private bool _isRunning;
    
    // WinAPI handles
    private IntPtr _notificationHandle = IntPtr.Zero;
    private IntPtr _windowHandle = IntPtr.Zero;
    private bool _disposed;

    // Device notification constants
    private const int DbtDevicearrival = 0x8000;
    private const int DbtDeviceremovecomplete = 0x8004;
    private const int DbtDevtypDeviceinterface = 2;
    private const int DeviceNotifyAllInterfaceClasses = 4;
    
    // GUIDs for device interfaces
    private static readonly Guid GuidDevinterfaceUsbHub = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");
    private static readonly Guid GuidDevinterfaceUsbDevice = new("a5dcbf10-6530-11d2-901f-00c04fb951ed");
    private static readonly Guid GuidDevinterfaceHid = new("4d1e55b2-f16f-11cf-88cb-001111000030");

    public WinApiDeviceMonitorService(
        ILogger<WinApiDeviceMonitorService> logger,
        IDeviceTrackerService deviceTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceTracker = deviceTracker ?? throw new ArgumentNullException(nameof(deviceTracker));
    }

    public bool IsRunning => _isRunning;

    public event EventHandler<UsbDevice>? DeviceConnected;
    public event EventHandler<string>? DeviceDisconnected;

    /// <summary>
    /// Starts the device monitoring service.
    /// </summary>
    public Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Device monitor is already running");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting USB device monitoring service");
        
        try
        {
            // Initialize notification
            InitializeDeviceNotification();
            
            // Start background polling as fallback
            _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _monitoringTask = MonitorLoopAsync(_monitoringCts.Token);
            
            _isRunning = true;
            
            _logger.LogInformation("USB device monitoring service started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start device monitoring service");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the device monitoring service.
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping USB device monitoring service");

        try
        {
            // Cancel monitoring
            if (_monitoringCts != null)
            {
                await _monitoringCts.CancelAsync();
            }

            // Wait for monitoring task to complete
            if (_monitoringTask != null)
            {
                await _monitoringTask.WaitAsync(TimeSpan.FromSeconds(5));
            }

            // Clean up notification handle
            if (_notificationHandle != IntPtr.Zero)
            {
                NativeMethods.UnregisterDeviceNotification(_notificationHandle);
                _notificationHandle = IntPtr.Zero;
            }

            _isRunning = false;
            _logger.LogInformation("USB device monitoring service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping device monitoring service");
        }
    }

    /// <summary>
    /// Initializes device notification registration.
    /// </summary>
    private void InitializeDeviceNotification()
    {
        try
        {
            // Create a message-only window for device notifications
            _windowHandle = NativeMethods.CreateMessageWindow();
            
            if (_windowHandle == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to create message window, using polling-only mode");
                return;
            }

            // Register for USB device interface notifications
            var notificationFilter = new DevBroadcastDeviceInterface
            {
                Size = Marshal.SizeOf<DevBroadcastDeviceInterface>(),
                DeviceType = DbtDevtypDeviceinterface,
                Reserved = 0,
                ClassGuid = GuidDevinterfaceUsbDevice
            };

            var filterPtr = Marshal.AllocHGlobal(notificationFilter.Size);
            try
            {
                Marshal.StructureToPtr(notificationFilter, filterPtr, false);
                
                _notificationHandle = NativeMethods.RegisterDeviceNotification(
                    _windowHandle,
                    filterPtr,
                    DeviceNotifyAllInterfaceClasses);

                if (_notificationHandle == IntPtr.Zero)
                {
                    _logger.LogWarning("Failed to register device notification");
                }
                else
                {
                    _logger.LogInformation("Device notification registered successfully");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(filterPtr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing device notification");
        }
    }

    /// <summary>
    /// Background monitoring loop for device polling.
    /// </summary>
    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        var pollInterval = TimeSpan.FromSeconds(2);
        var lastDevices = new HashSet<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(pollInterval, cancellationToken);
                
                // Get current devices
                var currentDevices = UsbDeviceEnumerator.EnumerateDevices();
                var currentIds = new HashSet<string>(currentDevices.Select(d => d.DeviceId));

                // Detect new devices
                foreach (var device in currentDevices)
                {
                    if (!lastDevices.Contains(device.DeviceId))
                    {
                        await OnDeviceConnectedAsync(device);
                    }
                }

                // Detect removed devices
                foreach (var deviceId in lastDevices)
                {
                    if (!currentIds.Contains(deviceId))
                    {
                        await OnDeviceDisconnectedAsync(deviceId);
                    }
                }

                lastDevices = currentIds;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in device monitoring loop");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles device connection event.
    /// </summary>
    private async Task OnDeviceConnectedAsync(UsbDevice device)
    {
        _logger.LogInformation(
            "Device connected: {DeviceName} (VID:{VendorId:X4}, PID:{ProductId:X4})",
            device.DeviceName,
            device.VendorId,
            device.ProductId);

        try
        {
            // Track the device
            var trackedDevice = await _deviceTracker.RegisterDeviceAsync(device);
            
            lock (_lock)
            {
                _connectedDevices[device.DeviceId] = trackedDevice;
            }

            // Raise event
            DeviceConnected?.Invoke(this, trackedDevice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device connection: {DeviceId}", device.DeviceId);
        }
    }

    /// <summary>
    /// Handles device disconnection event.
    /// </summary>
    private async Task OnDeviceDisconnectedAsync(string deviceId)
    {
        _logger.LogInformation("Device disconnected: {DeviceId}", deviceId);

        try
        {
            // Record disconnection
            await _deviceTracker.RecordDisconnectionAsync(deviceId);
            
            lock (_lock)
            {
                _connectedDevices.Remove(deviceId);
            }

            // Raise event
            DeviceDisconnected?.Invoke(this, deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing device disconnection: {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Gets all currently connected devices.
    /// </summary>
    public IReadOnlyList<UsbDevice> GetConnectedDevices()
    {
        lock (_lock)
        {
            return _connectedDevices.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets a specific device by ID.
    /// </summary>
    public UsbDevice? GetDeviceById(string deviceId)
    {
        lock (_lock)
        {
            return _connectedDevices.GetValueOrDefault(deviceId);
        }
    }

    /// <summary>
    /// Refreshes the device list.
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        _logger.LogDebug("Refreshing device list");
        
        var devices = UsbDeviceEnumerator.EnumerateDevices();
        
        lock (_lock)
        {
            foreach (var device in devices)
            {
                _connectedDevices[device.DeviceId] = device;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _monitoringCts?.Dispose();
        
        if (_notificationHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterDeviceNotification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;
        }
        
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
        
        _disposed = true;
    }
}

/// <summary>
/// USB device enumeration using SetupAPI.
/// </summary>
internal static class UsbDeviceEnumerator
{
    private static readonly Guid GuidUsbController = new("4d36e978-e325-11ce-bfc1-08002be10318");

    /// <summary>
    /// Enumerates all connected USB devices.
    /// </summary>
    public static List<UsbDevice> EnumerateDevices()
    {
        var devices = new List<UsbDevice>();

        try
        {
            // Get device info set for USB devices
            var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
                ref GuidUsbController,
                null,
                IntPtr.Zero,
                NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

            if (deviceInfoSet == NativeMethods.InvalidHandle)
            {
                return devices;
            }

            try
            {
                var deviceInfoData = new NativeMethods.SpDevInfoData();
                deviceInfoData.Size = Marshal.SizeOf<NativeMethods.SpDevInfoData>();

                for (uint i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
                {
                    try
                    {
                        var device = GetDeviceDetails(deviceInfoSet, deviceInfoData);
                        if (device != null)
                        {
                            devices.Add(device);
                        }
                    }
                    catch (Exception)
                    {
                        // Skip individual device errors
                    }
                }
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }
        catch (Exception)
        {
            // Return empty list on enumeration failure
        }

        return devices;
    }

    private static UsbDevice? GetDeviceDetails(
        IntPtr deviceInfoSet,
        NativeMethods.SpDevInfoData deviceInfoData)
    {
        // Get device ID
        var deviceId = GetDeviceProperty(deviceInfoSet, deviceInfoData, NativeMethods.SPDRP_HARDWAREID);
        
        // Get friendly name
        var deviceName = GetDeviceProperty(deviceInfoSet, deviceInfoData, NativeMethods.SPDRP_FRIENDLYNAME) 
                        ?? GetDeviceProperty(deviceInfoSet, deviceInfoData, NativeMethods.SPDRP_DEVICEDESC) 
                        ?? "Unknown Device";

        // Get manufacturer
        var manufacturer = GetDeviceProperty(deviceInfoSet, deviceInfoData, NativeMethods.SPDRP_MFG);

        // Get serial number
        var serialNumber = GetDeviceProperty(deviceInfoSet, deviceInfoData, NativeMethods.SPDRP_SERIALNUMBER);

        // Parse VID/PID from hardware ID
        var (vid, pid) = ParseVidPid(deviceId);

        if (vid == 0 || pid == 0)
        {
            return null;
        }

        return new UsbDevice
        {
            DeviceId = deviceId,
            VendorId = vid,
            ProductId = pid,
            DeviceName = deviceName,
            Manufacturer = manufacturer,
            SerialNumber = serialNumber,
            FirstConnected = DateTime.UtcNow,
            LastConnected = DateTime.UtcNow,
            IsConnected = true,
            IsAuthorized = false,
            Status = DeviceStatus.Connected,
            RiskLevel = RiskLevel.None
        };
    }

    private static string? GetDeviceProperty(
        IntPtr deviceInfoSet,
        NativeMethods.SpDevInfoData deviceInfoData,
        uint property)
    {
        try
        {
            if (NativeMethods.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                ref deviceInfoData,
                property,
                out var propertyType,
                out var propertyBuffer,
                out _))
            {
                if (propertyBuffer.Length > 0)
                {
                    var str = System.Text.Encoding.Unicode.GetString(propertyBuffer);
                    var nullIndex = str.IndexOf('\0');
                    return nullIndex > 0 ? str.Substring(0, nullIndex) : str.Trim();
                }
            }
        }
        catch
        {
            // Ignore property read errors
        }

        return null;
    }

    private static (ushort vid, ushort pid) ParseVidPid(string? hardwareId)
    {
        if (string.IsNullOrEmpty(hardwareId))
        {
            return (0, 0);
        }

        // Look for VID_ and PID_ patterns in hardware ID
        var vidMatch = System.Text.RegularExpressions.Regex.Match(hardwareId, @"VID_([0-9A-Fa-f]{4})");
        var pidMatch = System.Text.RegularExpressions.Regex.Match(hardwareId, @"PID_([0-9A-Fa-f]{4})");

        if (vidMatch.Success && pidMatch.Success)
        {
            return (
                Convert.ToUInt16(vidMatch.Groups[1].Value, 16),
                Convert.ToUInt16(pidMatch.Groups[1].Value, 16)
            );
        }

        return (0, 0);
    }
}

/// <summary>
/// Native Windows API methods for device monitoring.
/// </summary>
internal static class NativeMethods
{
    public static readonly IntPtr InvalidHandle = new(-1);
    
    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;
    
    public const uint SpdrpHardwareid = 0x00000001;
    public const uint SpdrpFriendlyname = 0x0000000C;
    public const uint SpdrpDeviceDesc = 0x00000000;
    public const uint SpdrpMfg = 0x0000000B;
    public const uint SpdrpSerialnumber = 0x00000005;

    public const uint SpdrpHardwareid = 0x00000001;
    public const uint SPDRP_FRIENDLYNAME = 0x0000000C;
    public const uint SPDRP_DEVICEDESC = 0x00000000;
    public const uint SPDRP_MFG = 0x0000000B;
    public const uint SPDRP_SERIALNUMBER = 0x00000005;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string? lpClassName,
        string? lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr RegisterDeviceNotification(
        IntPtr hRecipient,
        IntPtr notificationFilter,
        int flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterDeviceNotification(IntPtr handle);

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInfo(
        IntPtr deviceInfoSet,
        uint memberIndex,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        out byte[] propertyBuffer,
        out int propertyBufferSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    public struct SpDevInfoData
    {
        public int Size;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DevBroadcastDeviceInterface
    {
        public int Size;
        public int DeviceType;
        public int Reserved;
        public Guid ClassGuid;
        public short Reserved2;
    }

    /// <summary>
    /// Creates a message-only window for receiving device notifications.
    /// </summary>
    public static IntPtr CreateMessageWindow()
    {
        return CreateWindowEx(
            0,
            "STATIC",
            "DeviceMonitorWindow",
            0,
            0, 0, 0, 0,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }
}
