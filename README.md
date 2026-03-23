# HID Security v1

A hardened Windows security application with Electron + React frontend for device security management.

## Project Structure

```
HID Security v1/
├── Frontend/           # Electron + React desktop application
│   ├── electron/       # Electron main process & preload
│   ├── src/            # React renderer
│   │   ├── components/ # UI components
│   │   ├── pages/      # Application pages
│   │   ├── types/      # TypeScript types
│   │   └── mock/       # Mock data
│   └── dist/           # Build output
└── Backend/            # Security service (to be implemented)
```

## Frontend Features

- **Hardened Security**
  - nodeIntegration: disabled
  - contextIsolation: enabled
  - Strict Content Security Policy
  - IPC validation
  
- **Pages**
  - Login/MFA Authentication
  - Dashboard with statistics
  - Device Management
  - Alerts & Risk Events
  - Policy Viewer
  - Admin Actions
  - Settings

## Default Credentials

- **Username:** `admin`
- **Password:** `admin`
- **MFA Code:** `123456`

## Development

### Frontend

```bash
cd Frontend
npm install
npm run electron:dev    # Development mode
npm run build           # Production build
```

### Backend

Backend implementation pending.

## Tech Stack

- **Frontend:** React 18, TypeScript, Electron, Vite, React Router
- **Backend:** (To be implemented)

## Security Architecture

```
┌─────────────────────────────────────┐
│     Electron Main Process           │
│  - IPC handlers with validation     │
│  - CSP enforcement                  │
└─────────────────────────────────────┘
                    ↕ IPC (validated)
┌─────────────────────────────────────┐
│       Preload Script                │
│  - contextBridge exposure           │
│  - Channel whitelist                │
└─────────────────────────────────────┘
                     contextBridge
┌─────────────────────────────────────┐
│      React Renderer                 │
│  - Isolated from Node.js            │
│  - Error boundaries                 │
└─────────────────────────────────────┘
```

## License

MIT
