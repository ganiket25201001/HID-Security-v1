# HID Security Service - Backend

A secure .NET 8 Windows Service for USB/HID device security monitoring and threat detection.

## Overview

This Windows Service provides the core security engine for the HID Security v1 product. It runs in the background, monitors USB device connections, evaluates security policies, and prepares data for AI-based threat scoring.

## Features

- **Real-time USB Device Monitoring** - WinAPI-based device detection using `RegisterDeviceNotification`
- **Device State Tracking** - Persistent device history and connection records
- **Policy Evaluation Engine** - Rule-based security policy enforcement
- **Structured Logging** - File and Windows Event Log integration for SIEM compliance
- **Secure IPC** - Named Pipe communication with ACL-based access control
- **DPAPI Encryption** - Secure configuration storage using Windows Data Protection API
- **Service Recovery** - Automatic restart on failure with configurable actions
- **AI Integration Hooks** - Placeholder interfaces for ML-based threat scoring

## Architecture

```
HIDSecurityService/
├── Core/
│   ├── Models/           # Device and data models
│   ├── Events/           # Security event definitions
│   └── Interfaces/       # Service contracts
├── Services/
│   ├── DeviceMonitoring/ # USB device detection (WinAPI)
│   ├── DeviceTracking/   # Device state persistence
│   ├── Logging/          # Security event logging
│   ├── Policy/           # Policy evaluation engine
│   ├── AI/               # Threat scoring (placeholder)
│   └── IPC/              # Named Pipe communication
├── Security/             # Security helpers (DPAPI)
├── Configuration/        # Configuration management
└── Installers/           # Service installation
```

## Security Features

### Service Hardening
- Runs with least privilege (configurable account)
- No direct filesystem exposure to external inputs
- All IPC messages validated against schema
- Configuration integrity checks with SHA-256 hashing

### Named Pipe Security
- ACL-based access control
- Only Administrators, SYSTEM, and configured users/groups can connect
- Message encryption support (configurable)

### Data Protection
- Sensitive configuration values encrypted with DPAPI
- Options for CurrentUser or LocalMachine scope
- Integrity hash verification on startup

### Logging & Audit
- Structured JSON logging for SIEM integration
- Windows Event Log integration (Application channel)
- Log rotation and retention policies
- Tamper-evident log integrity (future)

## Installation

### Prerequisites
- Windows 10/11 or Windows Server 2019+
- .NET 8.0 Runtime
- Administrator privileges for installation

### Build

```bash
cd Backend
dotnet restore
dotnet build --configuration Release
```

### Install Service

```bash
# Run as Administrator
cd src\HIDSecurityService\bin\Release\net8.0\win-x64
HIDSecurityService.exe install
```

### Verify Installation

```bash
# Check service status
sc query HIDSecurityService

# Or use built-in command
HIDSecurityService.exe status
```

### Uninstall Service

```bash
# Run as Administrator
HIDSecurityService.exe uninstall
```

## Configuration

Configuration is stored in `appsettings.json`:

```json
{
  "ServiceConfiguration": {
    "Logging": {
      "LogPath": "logs",
      "EventLogSource": "HIDSecurityService",
      "MinimumLogLevel": "Information",
      "RetentionDays": 90,
      "EnableEventLog": true
    },
    "Monitoring": {
      "PollingIntervalMs": 2000,
      "MaxTrackedDevices": 1000
    },
    "Ipc": {
      "PipeName": "HIDSecurityService",
      "MaxConnections": 10,
      "AllowedGroups": ["BUILTIN\\Administrators"]
    },
    "Security": {
      "EnableIntegrityChecks": true,
      "EnableDpapiEncryption": true
    }
  }
}
```

### Encrypting Sensitive Values

```csharp
// Encrypt a value
var encrypted = DpapiHelper.Encrypt("MySecretPassword");

// Decrypt a value
var decrypted = DpapiHelper.Decrypt(encrypted);
```

## IPC Communication

The service exposes a Named Pipe for frontend communication:

```
\\.\pipe\HIDSecurityService
```

### Message Format

```json
{
  "MessageId": "unique-id",
  "MessageType": "DeviceListRequest",
  "Timestamp": "2024-03-24T10:00:00Z",
  "Payload": { ... },
  "IsRequest": true
}
```

### Available Message Types

| Type | Description |
|------|-------------|
| `DeviceListRequest` | Get all connected devices |
| `DeviceStatusRequest` | Get status of specific device |
| `AlertListRequest` | Get recent security alerts |
| `PolicyGetRequest` | Get active policy |
| `AdminActionRequest` | Execute admin action |
| `ServiceStatusRequest` | Get service health |

## Policy Rules

Default policy rules are applied on startup. Custom policies can be loaded from JSON:

```json
{
  "PolicyId": "custom-policy-1",
  "Name": "High Security Policy",
  "Rules": [
    {
      "RuleId": "block-unknown-storage",
      "Name": "Block Unknown Storage Devices",
      "Enabled": true,
      "Priority": 10,
      "Condition": {
        "BlockedDeviceClasses": [8],
        "RequireSerialNumber": true
      },
      "Action": "Block"
    }
  ]
}
```

### Device Classes

| Class | Code | Description |
|-------|------|-------------|
| HID | 0x03 | Keyboards, Mice |
| Mass Storage | 0x08 | USB Drives |
| Video | 0x0E | Webcams |
| Vendor Specific | 0xFF | Custom devices |

## Logging

### Log Locations

- **File Logs**: `logs/service-YYYYMMDD.log`
- **Bootstrap Logs**: `logs/bootstrap-YYYYMMDD.log`
- **Security Events**: `logs/security-events.log`
- **Windows Event Log**: Application > HIDSecurityService

### Log Format

```json
{
  "Timestamp": "2024-03-24T10:00:00.000Z",
  "EventId": "abc123",
  "EventType": "DeviceConnectedEvent",
  "Severity": "Informational",
  "Category": "DeviceConnection",
  "Title": "Device Connected: USB Mouse",
  "Description": "VID:046D PID:C52B",
  "DeviceId": "USB\\VID_046D&PID_C52B\\...",
  "Data": {
    "PolicyAllowed": true,
    "ThreatScore": 0.1
  }
}
```

## Development

### Run as Console App (Debug)

```bash
dotnet run --configuration Debug
```

### Run Tests

```bash
dotnet test
```

### Publish for Production

```bash
dotnet publish --configuration Release \
  --runtime win-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

### Code Signing

Before production deployment, sign the binary:

```bash
# Configure signing properties
set CertificatePath=C:\certs\codesigning.pfx
set CertificatePassword=your-password

# Build with signing
dotnet publish -c Release -p:EnableCodeSigning=true
```

## Service Recovery

The service is configured with automatic recovery:

| Failure | Action | Delay |
|---------|--------|-------|
| First | Restart | 1 minute |
| Second | Restart | 5 minutes |
| Third+ | Restart | 10 minutes |

Reset period: 24 hours

## Integration Points

### Frontend Integration

The React frontend connects via Named Pipe:

```typescript
// Frontend IPC call example
const devices = await window.electronAPI.invoke(
  window.electronAPI.channels.DEVICE_LIST
);
```

### AI Service Integration

Implement `IThreatScoringService` for ML integration:

```csharp
public class MlThreatScoringService : IThreatScoringService
{
    public async Task<ThreatAnalysisResult> AnalyzeDeviceAsync(UsbDevice device)
    {
        // Load ML model and score device
        // ...
    }
}
```

### SIEM Integration

Forward logs to your SIEM:
- Configure Windows Event Log forwarding
- Parse JSON logs from `logs/security-events.log`
- Use Event IDs for filtering (generated per category)

## Troubleshooting

### Service Won't Start

1. Check Windows Event Log for errors
2. Verify .NET 8.0 Runtime is installed
3. Ensure service account has required permissions
4. Check configuration file syntax

### Device Detection Issues

1. Verify you're running on Windows (WinAPI required)
2. Check device manager for device visibility
3. Review `logs/service-.log` for detection errors

### IPC Connection Failed

1. Verify service is running
2. Check pipe name matches configuration
3. Ensure client has required ACL permissions
4. Review firewall rules (if remote)

## API Reference

### IDeviceMonitorService

```csharp
Task StartMonitoringAsync(CancellationToken ct);
Task StopMonitoringAsync();
IReadOnlyList<UsbDevice> GetConnectedDevices();
event EventHandler<UsbDevice> DeviceConnected;
event EventHandler<string> DeviceDisconnected;
```

### IPolicyEvaluationService

```csharp
Task<PolicyDecision> EvaluateDeviceAsync(UsbDevice device);
Task<PolicyDecision> EvaluateActionAsync(string deviceId, string action);
Task<PolicyDocument?> GetActivePolicyAsync();
Task ReloadPoliciesAsync();
```

### ISecurityEventLogger

```csharp
Task LogEventAsync(SecurityEvent securityEvent);
void LogToEventLog(SecurityEvent securityEvent);
Task<IReadOnlyList<SecurityEvent>> GetRecentEventsAsync(int count);
Task ExportEventsAsync(string filePath, DateTime? from, DateTime? to);
```

## License

MIT License - See LICENSE file for details.

## Support

For issues and feature requests, please open an issue on GitHub.


----------------------------------------------------------------

# Build
cd Backend
dotnet build

# Install service (as Admin)
HIDSecurityService.exe install

# Check status
HIDSecurityService.exe status

# Uninstall
HIDSecurityService.exe uninstall

# Run tests
dotnet test