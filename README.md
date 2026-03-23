# HID Security v1

A comprehensive Windows-based USB/HID security product with a hardened Electron + React frontend and a secure .NET 8 Windows Service backend.

## Overview

HID Security v1 provides real-time monitoring, policy enforcement, and threat detection for USB/HID devices on Windows systems. It consists of two main components:

- **Frontend**: Hardened Electron + React desktop application for security console management
- **Backend**: .NET 8 Windows Service for USB device monitoring and security enforcement

## Project Structure

```
HID Security v1/
├── Frontend/                   # Electron + React Desktop Application
│   ├── electron/               # Electron main process (hardened)
│   ├── src/                    # React renderer
│   │   ├── components/         # UI components
│   │   ├── pages/              # Application pages
│   │   ├── types/              # TypeScript types
│   │   └── mock/               # Mock data
│   └── dist/                   # Build output
│
├── Backend/                    # .NET 8 Windows Service
│   ├── src/HIDSecurityService/ # Main service implementation
│   │   ├── Core/               # Models, events, interfaces
│   │   ├── Services/           # Device monitoring, logging, policy, AI
│   │   ├── Configuration/      # DPAPI encryption
│   │   └── Installers/         # Service installation
│   ├── tests/                  # Unit tests
│   └── signing/                # Code signing configuration
│
└── README.md                   # This file
```

## Features

### Frontend (Electron + React)
- **Hardened Security**
  - `nodeIntegration: false` - No Node.js access from renderer
  - `contextIsolation: true` - Isolated execution context
  - Strict Content Security Policy (CSP)
  - IPC validation with whitelisted channels
  - Error boundaries for graceful failure handling

- **Pages**
  - Login/MFA Authentication (default: admin/admin, MFA: 123456)
  - Dashboard with statistics and alerts
  - Device Management with filtering
  - Alerts & Risk Events
  - Policy Viewer
  - Admin Actions
  - Settings

- **UI Features**
  - Dark mode security console theme
  - Responsive layout
  - Real-time service status monitoring
  - Device cards with risk badges
  - Alert summaries and recent events

### Backend (.NET 8 Windows Service)
- **Device Monitoring**
  - WinAPI-based USB detection (`RegisterDeviceNotification`)
  - Real-time device connection/disconnection events
  - Device state tracking and persistence

- **Security Features**
  - Rule-based policy evaluation
  - DPAPI configuration encryption
  - Named Pipe IPC with ACL-based access control
  - Configuration integrity checks (SHA-256)
  - Service auto-recovery on failure

- **Logging & Audit**
  - Structured JSON logging
  - Windows Event Log integration
  - Log rotation and retention
  - SIEM-ready event format

- **AI Integration**
  - Placeholder interfaces for ML threat scoring
  - Heuristic-based risk assessment
  - Behavior pattern detection hooks

## Quick Start

### Prerequisites

- **Frontend**: Node.js 18+, npm 9+
- **Backend**: .NET 8.0 SDK, Windows 10/11 or Windows Server 2019+

### Frontend Setup

```bash
cd Frontend
npm install
npm run electron:dev
```

**Default Login:**
- Username: `admin`
- Password: `admin`
- MFA Code: `123456`

### Backend Setup

```bash
cd Backend

# Build
dotnet build --configuration Release

# Install service (Run as Administrator)
cd src\HIDSecurityService\bin\Release\net8.0\win-x64
HIDSecurityService.exe install

# Check status
HIDSecurityService.exe status

# View logs
# Event Viewer > Windows Logs > Application > HIDSecurityService
```

### Build for Production

**Frontend:**
```bash
cd Frontend
npm run build
```

**Backend:**
```bash
cd Backend
dotnet publish --configuration Release --runtime win-x64 --self-contained
```

## Security Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (Electron)                   │
│  ┌─────────────────────────────────────────────────┐    │
│  │  React Renderer (Isolated)                       │    │
│  │  - No Node.js access                             │    │
│  │  - Error Boundaries                              │    │
│  └─────────────────────────────────────────────────┘    │
│                          ↕ IPC (validated)               │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Preload Script (Secure Bridge)                  │    │
│  │  - Channel whitelist                             │    │
│  │  - Message validation                            │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                          ↕ Named Pipe (ACL secured)
┌─────────────────────────────────────────────────────────┐
│                  Backend (Windows Service)               │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Device Monitor (WinAPI)                         │    │
│  │  - RegisterDeviceNotification                    │    │
│  │  - SetupAPI enumeration                          │    │
│  └─────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Policy Engine                                   │    │
│  │  - Rule evaluation                               │    │
│  │  - Device authorization                          │    │
│  └─────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────┐    │
│  │  Security Event Logger                           │    │
│  │  - File logging (JSON)                           │    │
│  │  - Windows Event Log                             │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## IPC Communication

### Frontend → Backend (Named Pipe)

```
\\.\pipe\HIDSecurityService
```

### Message Types

| Type | Description |
|------|-------------|
| `DeviceListRequest` | Get all connected devices |
| `DeviceStatusRequest` | Get status of specific device |
| `AlertListRequest` | Get recent security alerts |
| `PolicyGetRequest` | Get active policy |
| `AdminActionRequest` | Execute admin action |
| `ServiceStatusRequest` | Get service health |

### Example IPC Call (Frontend)

```typescript
const devices = await window.electronAPI.invoke(
  window.electronAPI.channels.DEVICE_LIST
);
```

## Configuration

### Frontend Configuration

No additional configuration needed. Default settings work out of the box.

### Backend Configuration

Edit `Backend/src/HIDSecurityService/appsettings.json`:

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

## Policy Rules

Default policy rules are applied on startup. Example custom policy:

```json
{
  "PolicyId": "high-security",
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

### USB Device Classes

| Class | Code | Description |
|-------|------|-------------|
| HID | 0x03 | Keyboards, Mice |
| Mass Storage | 0x08 | USB Drives |
| Video | 0x0E | Webcams |
| Network | 0x0A | Network Adapters |
| Vendor Specific | 0xFF | Custom devices |

## Development

### Frontend Development

```bash
cd Frontend
npm install
npm run electron:dev    # Development with hot reload
npm run build           # Production build
npm run typecheck       # TypeScript validation
npm run lint            # ESLint check
```

### Backend Development

```bash
cd Backend
dotnet restore
dotnet build
dotnet test             # Run unit tests
dotnet run              # Run as console app (debug)
```

### Running Both Together

1. Start Backend service: `HIDSecurityService.exe install` then `sc start HIDSecurityService`
2. Start Frontend: `npm run electron:dev`

## Service Management

### Install Service

```bash
# Run as Administrator
cd Backend\src\HIDSecurityService\bin\Release\net8.0\win-x64
HIDSecurityService.exe install
```

### Check Status

```bash
sc query HIDSecurityService
# or
HIDSecurityService.exe status
```

### Stop Service

```bash
sc stop HIDSecurityService
```

### Uninstall Service

```bash
# Run as Administrator
HIDSecurityService.exe uninstall
```

### Service Recovery

The service is configured with automatic recovery:

| Failure | Action | Delay |
|---------|--------|-------|
| First | Restart | 1 minute |
| Second | Restart | 5 minutes |
| Third+ | Restart | 10 minutes |

## Logging

### Log Locations

- **Frontend**: Console (DevTools)
- **Backend File Logs**: `Backend/src/HIDSecurityService/bin/<config>/logs/`
- **Windows Event Log**: Application > HIDSecurityService

### Log Format (JSON)

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

## Testing

### Frontend Tests

```bash
cd Frontend
npm run typecheck
npm run lint
```

### Backend Tests

```bash
cd Backend
dotnet test
```

## Code Signing

For production deployment, sign the backend binary:

```bash
# Configure signing properties
set CertificatePath=C:\certs\codesigning.pfx
set CertificatePassword=your-password

# Build with signing
cd Backend
dotnet publish -c Release -p:EnableCodeSigning=true
```

## Troubleshooting

### Frontend Issues

**App won't start:**
```bash
cd Frontend
npm install
npm run build
npm run electron:start
```

**Login not working:**
- Use credentials: `admin` / `admin`
- MFA code: `123456`

### Backend Issues

**Service won't start:**
1. Check Event Viewer > Windows Logs > Application
2. Verify .NET 8.0 Runtime is installed
3. Run as Administrator

**Device detection not working:**
1. Ensure you're on Windows (WinAPI required)
2. Check Device Manager for device visibility
3. Review service logs

**IPC connection failed:**
1. Verify service is running: `sc query HIDSecurityService`
2. Check pipe name matches: `HIDSecurityService`
3. Ensure client has required permissions

## API Reference

### Frontend IPC Channels

```typescript
// Available channels in window.electronAPI.channels
AUTH_LOGIN
AUTH_LOGOUT
AUTH_STATUS
AUTH_MFA_VERIFY
DEVICE_LIST
DEVICE_STATUS
ALERT_LIST
ALERT_ACKNOWLEDGE
POLICY_GET
POLICY_LIST
ADMIN_ACTION
SETTINGS_GET
SETTINGS_UPDATE
SERVICE_STATUS
SERVICE_HEALTH
```

### Backend Services

```csharp
IDeviceMonitorService    // USB device detection
IDeviceTrackerService    // Device state persistence
ISecurityEventLogger     // Event logging
IPolicyEvaluationService // Policy enforcement
IThreatScoringService    // AI threat scoring (placeholder)
IIpcCommunicationService // Named Pipe server
IConfigurationManager    // Configuration management
```

## Roadmap

### Phase 1 (Current)
- [x] Hardened Electron frontend
- [x] .NET 8 Windows Service backend
- [x] USB device monitoring
- [x] Policy evaluation engine
- [x] Secure IPC communication

### Phase 2 (Future)
- [ ] ML-based threat scoring
- [ ] Real-time behavior analysis
- [ ] Advanced policy editor
- [ ] Remote management console
- [ ] Cloud sync for policies

## License

MIT License - See LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests
5. Submit a pull request

## Support

For issues and feature requests, please open an issue on GitHub.

## Acknowledgments

- Electron Team
- .NET Team
- React Team
- Serilog Team
