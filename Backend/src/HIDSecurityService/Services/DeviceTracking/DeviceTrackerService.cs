using System.Collections.Concurrent;
using HIDSecurityService.Core.Interfaces;
using HIDSecurityService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HIDSecurityService.Services.DeviceTracking;

/// <summary>
/// In-memory device tracking service with persistence support.
/// </summary>
public sealed class DeviceTrackerService : IDeviceTrackerService
{
    private readonly ILogger<DeviceTrackerService> _logger;
    private readonly MonitoringConfiguration _config;
    private readonly ConcurrentDictionary<string, UsbDevice> _devices = new();
    private readonly ConcurrentDictionary<string, List<DeviceConnectionRecord>> _connectionHistory = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _storagePath;

    public DeviceTrackerService(
        ILogger<DeviceTrackerService> logger,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value.Monitoring;
        _storagePath = Path.Combine(AppContext.BaseDirectory, "data", "devices");
        
        Directory.CreateDirectory(_storagePath);
    }

    /// <summary>
    /// Registers a newly connected device.
    /// </summary>
    public Task<UsbDevice> RegisterDeviceAsync(UsbDevice device)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));

        var fingerprint = device.GetFingerprint();
        
        // Check if this is a known device
        var existingDevice = _devices.Values.FirstOrDefault(d => d.GetFingerprint() == fingerprint);
        
        if (existingDevice != null)
        {
            // Update existing device
            existingDevice.IsConnected = true;
            existingDevice.LastConnected = DateTime.UtcNow;
            existingDevice.LastDisconnected = null;
            existingDevice.Status = DeviceStatus.Connected;
            
            // Copy over any policy decisions
            device.IsAuthorized = existingDevice.IsAuthorized;
            device.Status = existingDevice.Status;
            device.RiskLevel = existingDevice.RiskLevel;
        }
        else
        {
            // New device - enforce max limit
            if (_devices.Count >= _config.MaxTrackedDevices)
            {
                _logger.LogWarning("Maximum device limit reached ({Max}). Consider cleanup.", _config.MaxTrackedDevices);
            }
            
            device.FirstConnected = DateTime.UtcNow;
        }

        _devices[device.DeviceId] = device;
        
        // Record connection
        RecordConnection(device.DeviceId);
        
        // Persist to disk
        _ = PersistDeviceAsync(device);

        _logger.LogDebug(
            "Device registered: {DeviceId} (Fingerprint: {Fingerprint})",
            device.DeviceId,
            fingerprint);

        return Task.FromResult(device);
    }

    /// <summary>
    /// Updates device status.
    /// </summary>
    public Task UpdateDeviceStatusAsync(string deviceId, DeviceStatus status)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.Status = status;
            _ = PersistDeviceAsync(device);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Records device disconnection.
    /// </summary>
    public Task RecordDisconnectionAsync(string deviceId)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.IsConnected = false;
            device.LastDisconnected = DateTime.UtcNow;
            device.Status = DeviceStatus.Disconnected;
            
            // Update connection history
            RecordDisconnection(deviceId);
            
            _ = PersistDeviceAsync(device);
            
            _logger.LogDebug("Device disconnection recorded: {DeviceId}", deviceId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets device by fingerprint.
    /// </summary>
    public Task<UsbDevice?> GetDeviceByFingerprintAsync(string fingerprint)
    {
        var device = _devices.Values.FirstOrDefault(d => d.GetFingerprint() == fingerprint);
        return Task.FromResult(device);
    }

    /// <summary>
    /// Gets all tracked devices.
    /// </summary>
    public Task<IReadOnlyList<UsbDevice>> GetAllDevicesAsync()
    {
        return Task.FromResult<IReadOnlyList<UsbDevice>>(_devices.Values.ToList());
    }

    /// <summary>
    /// Gets device connection history.
    /// </summary>
    public Task<IReadOnlyList<DeviceConnectionRecord>> GetConnectionHistoryAsync(string deviceId, int limit = 100)
    {
        if (_connectionHistory.TryGetValue(deviceId, out var history))
        {
            return Task.FromResult<IReadOnlyList<DeviceConnectionRecord>>(
                history.OrderByDescending(h => h.ConnectedAt).Take(limit).ToList());
        }

        return Task.FromResult<IReadOnlyList<DeviceConnectionRecord>>(Array.Empty<DeviceConnectionRecord>());
    }

    /// <summary>
    /// Clears disconnected devices older than the specified date.
    /// </summary>
    public async Task<int> CleanupOldDevicesAsync(DateTime olderThan)
    {
        await _lock.WaitAsync();
        try
        {
            var removed = 0;
            var devicesToRemove = _devices.Values
                .Where(d => !d.IsConnected && d.LastDisconnected.HasValue && d.LastDisconnected.Value < olderThan)
                .ToList();

            foreach (var device in devicesToRemove)
            {
                if (_devices.TryRemove(device.DeviceId, out _))
                {
                    removed++;
                    _logger.LogDebug("Cleaned up old device: {DeviceId}", device.DeviceId);
                }
            }

            if (removed > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old devices", removed);
            }

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void RecordConnection(string deviceId)
    {
        var record = new DeviceConnectionRecord
        {
            DeviceId = deviceId,
            ConnectedAt = DateTime.UtcNow,
            ComputerName = Environment.MachineName,
            SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId
        };

        _connectionHistory.AddOrUpdate(
            deviceId,
            new List<DeviceConnectionRecord> { record },
            (_, list) =>
            {
                list.Add(record);
                return list;
            });
    }

    private void RecordDisconnection(string deviceId)
    {
        if (_connectionHistory.TryGetValue(deviceId, out var history))
        {
            var lastConnection = history.OrderByDescending(h => h.ConnectedAt).FirstOrDefault();
            if (lastConnection != null && lastConnection.DisconnectedAt == null)
            {
                lastConnection.DisconnectedAt = DateTime.UtcNow;
            }
        }
    }

    private async Task PersistDeviceAsync(UsbDevice device)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, $"{device.DeviceId.GetHashCode():X8}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(device, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist device: {DeviceId}", device.DeviceId);
        }
    }

    /// <summary>
    /// Loads persisted devices from disk.
    /// </summary>
    public async Task LoadPersistedDevicesAsync()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
            {
                return;
            }

            var files = Directory.GetFiles(_storagePath, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var device = System.Text.Json.JsonSerializer.Deserialize<UsbDevice>(json);
                    if (device != null)
                    {
                        _devices[device.DeviceId] = device;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load device from: {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} persisted devices", _devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load persisted devices");
        }
    }
}
