# HID Security Console - Frontend

A hardened Electron + React frontend for Windows security product.

## Security Features

This frontend is built with security as a primary concern:

- **nodeIntegration: disabled** - Renderer cannot access Node.js APIs directly
- **contextIsolation: enabled** - Renderer runs in isolated context
- **remote: disabled** - No remote module access
- **Strict CSP** - Content Security Policy prevents unauthorized resource loading
- **IPC Validation** - All IPC messages are validated against defined types
- **No filesystem/shell/process exposure** - Renderer is isolated from privileged APIs
- **Error Boundaries** - Graceful handling of component errors and backend disconnects

## Tech Stack

- **React 18** - UI framework
- **TypeScript** - Type safety
- **Electron** - Desktop shell
- **Vite** - Build tool
- **React Router** - Navigation

## Project Structure

```
Frontend/
├── electron/
│   ├── main/           # Electron main process
│   │   └── index.ts
│   └── preload/        # Preload script (secure bridge)
│       └── index.ts
├── src/
│   ├── components/     # Reusable UI components
│   │   ├── ErrorBoundary.tsx
│   │   ├── BackendDisconnectBoundary.tsx
│   │   ├── Sidebar.tsx
│   │   ├── TopBar.tsx
│   │   └── AppLayout.tsx
│   ├── pages/          # Page components
│   │   ├── LoginPage.tsx
│   │   ├── DashboardPage.tsx
│   │   ├── DevicesPage.tsx
│   │   ├── AlertsPage.tsx
│   │   ├── PolicyPage.tsx
│   │   ├── AdminPage.tsx
│   │   └── SettingsPage.tsx
│   ├── types/          # TypeScript types
│   │   ├── ipc-types.ts    # IPC channel definitions
│   │   └── electron.d.ts   # Electron API types
│   ├── mock/           # Mock data for development
│   │   └── data.ts
│   ├── styles/         # Global styles
│   │   └── global.css
│   ├── hooks/          # Custom React hooks
│   ├── context/        # React context providers
│   ├── assets/         # Static assets
│   ├── App.tsx         # Main app component
│   └── main.tsx        # Entry point
├── public/             # Static public assets
├── package.json
├── tsconfig.json
├── tsconfig.electron.json
├── vite.config.ts
└── eslint.config.js
```

## Development

### Install Dependencies

```bash
npm install
```

### Run in Development Mode

```bash
npm run electron:dev
```

This starts both Vite dev server and Electron.

### Build for Production

```bash
npm run build
```

### Type Checking

```bash
npm run typecheck
```

### Linting

```bash
npm run lint
```

## IPC Communication

All communication between renderer and main process goes through defined IPC channels:

```typescript
// Example usage in React component
const status = await window.electronAPI.invoke(
  window.electronAPI.channels.SERVICE_STATUS
);

// Subscribe to events
const unsubscribe = window.electronAPI.on(
  window.electronAPI.channels.ALERT_LIST,
  (data) => {
    console.log('New alerts:', data);
  }
);
```

See `src/types/ipc-types.ts` for all available channels and payload types.

## Pages

1. **Login/MFA** - Authentication with default credentials (admin/admin, MFA: 123456)
2. **Dashboard** - Overview with statistics, alerts, and device summary
3. **Devices** - Connected device list with filtering and details
4. **Alerts** - Security alerts with acknowledgment and dismissal
5. **Policy** - Security policy viewer
6. **Admin** - Administrative actions and tools
7. **Settings** - Application configuration

## Security Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Electron Main Process                    │
│  - IPC handlers with validation                              │
│  - CSP enforcement                                           │
│  - No direct renderer access to system APIs                  │
└─────────────────────────────────────────────────────────────┘
                              ↕ IPC (validated)
┌─────────────────────────────────────────────────────────────┐
│                      Preload Script                          │
│  - contextBridge exposure                                    │
│  - Channel whitelist                                         │
│  - Message validation                                        │
└─────────────────────────────────────────────────────────────┘
                              ↕ contextBridge
┌─────────────────────────────────────────────────────────────┐
│                    React Renderer                            │
│  - Isolated from Node.js                                     │
│  - No filesystem/shell/process access                        │
│  - Error boundaries for graceful failures                    │
└─────────────────────────────────────────────────────────────┘
```

## Future Integration

This frontend is designed to connect to a backend service through secure IPC:

1. Backend communication channels are defined in `ipc-types.ts`
2. Mock data allows UI development before backend is ready
3. Error boundaries handle backend disconnects gracefully
4. Service status is monitored and displayed in the top bar

## License

MIT
