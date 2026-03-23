/**
 * IPC Channel Definitions and Payload Interfaces
 * 
 * This file defines all allowed IPC channels and their payload types.
 * Both main and renderer processes must use these types for validation.
 * 
 * SECURITY: All IPC messages are validated against these types in the preload script.
 */

// ============================================================================
// Channel Names - Single source of truth for all IPC channels
// ============================================================================

export const IPC_CHANNELS = {
  // App lifecycle
  APP_READY: 'app:ready',
  APP_VERSION: 'app:version',
  APP_QUIT: 'app:quit',
  
  // Backend communication (via secure bridge)
  BACKEND_SEND: 'backend:send',
  BACKEND_RESPONSE: 'backend:response',
  BACKEND_ERROR: 'backend:error',
  
  // Device management (read-only from renderer)
  DEVICE_LIST: 'device:list',
  DEVICE_STATUS: 'device:status',
  DEVICE_UPDATE: 'device:update',
  
  // Alerts and events
  ALERT_LIST: 'alert:list',
  ALERT_ACKNOWLEDGE: 'alert:acknowledge',
  ALERT_DISMISS: 'alert:dismiss',
  
  // Policy management (read-only from renderer)
  POLICY_GET: 'policy:get',
  POLICY_LIST: 'policy:list',
  
  // Admin actions (requires elevated privileges)
  ADMIN_ACTION: 'admin:action',
  ADMIN_STATUS: 'admin:status',
  
  // Settings
  SETTINGS_GET: 'settings:get',
  SETTINGS_UPDATE: 'settings:update',
  
  // Authentication
  AUTH_LOGIN: 'auth:login',
  AUTH_LOGOUT: 'auth:logout',
  AUTH_STATUS: 'auth:status',
  AUTH_MFA_VERIFY: 'auth:mfa:verify',
  
  // Service status
  SERVICE_STATUS: 'service:status',
  SERVICE_HEALTH: 'service:health',
  
  // Error handling
  RENDERER_ERROR: 'renderer:error',
} as const;

export type ChannelName = typeof IPC_CHANNELS[keyof typeof IPC_CHANNELS];

// ============================================================================
// Payload Interfaces - Type-safe IPC message contracts
// ============================================================================

// Base interface for all IPC messages
export interface IPCMessage<T = unknown> {
  channel: ChannelName;
  payload: T;
  timestamp: number;
  messageId: string;
}

// App lifecycle payloads
export interface AppVersionPayload {
  version: string;
  buildNumber: string;
  electronVersion: string;
}

export interface AppReadyPayload {
  isReady: boolean;
  environment: 'development' | 'production';
}

// Backend communication payloads
export interface BackendSendPayload {
  endpoint: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  data?: Record<string, unknown>;
  requestId: string;
}

export interface BackendResponsePayload {
  requestId: string;
  status: number;
  data?: Record<string, unknown>;
  error?: string;
}

export interface BackendErrorPayload {
  requestId: string;
  error: string;
  code: string;
}

// Device payloads
export interface DeviceInfo {
  deviceId: string;
  deviceName: string;
  deviceType: 'keyboard' | 'mouse' | 'storage' | 'network' | 'other';
  vendorId: string;
  productId: string;
  serialNumber?: string;
  isConnected: boolean;
  isAuthorized: boolean;
  lastSeen: string;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
}

export interface DeviceListPayload {
  devices: DeviceInfo[];
  totalCount: number;
  timestamp: number;
}

export interface DeviceStatusPayload {
  deviceId: string;
  status: 'connected' | 'disconnected' | 'blocked' | 'quarantined';
  lastActivity: string;
}

export interface DeviceUpdatePayload {
  deviceId: string;
  action: 'authorize' | 'block' | 'quarantine';
  reason?: string;
}

// Alert payloads
export interface AlertEvent {
  alertId: string;
  type: 'device_connected' | 'policy_violation' | 'suspicious_activity' | 'malware_detected' | 'unauthorized_access';
  severity: 'low' | 'medium' | 'high' | 'critical';
  title: string;
  description: string;
  deviceId?: string;
  userId?: string;
  timestamp: string;
  acknowledged: boolean;
  dismissed: boolean;
  metadata?: Record<string, unknown>;
}

export interface AlertListPayload {
  alerts: AlertEvent[];
  totalCount: number;
  unreadCount: number;
  timestamp: number;
}

export interface AlertActionPayload {
  alertId: string;
  action: 'acknowledge' | 'dismiss';
  reason?: string;
}

// Policy payloads
export interface PolicyRule {
  ruleId: string;
  name: string;
  description: string;
  enabled: boolean;
  action: 'allow' | 'block' | 'quarantine' | 'notify';
  conditions: Record<string, unknown>;
  priority: number;
}

export interface PolicyConfig {
  policyId: string;
  name: string;
  version: string;
  rules: PolicyRule[];
  lastUpdated: string;
  enforcedBy: string;
}

export interface PolicyListPayload {
  policies: PolicyConfig[];
  activePolicyId: string;
  timestamp: number;
}

// Admin action payloads
export interface AdminActionPayload {
  actionType: 'restart_service' | 'reload_policy' | 'flush_logs' | 'export_data' | 'system_scan';
  parameters?: Record<string, unknown>;
  reason: string;
  adminId: string;
}

export interface AdminActionResponse {
  success: boolean;
  actionType: string;
  result?: Record<string, unknown>;
  error?: string;
}

export interface AdminStatusPayload {
  isAdmin: boolean;
  privileges: string[];
  sessionExpiry: string;
}

// Settings payloads
export interface AppSettings {
  theme: 'dark' | 'light';
  notificationsEnabled: boolean;
  autoRefreshInterval: number;
  language: string;
  showSystemTray: boolean;
}

export interface SettingsGetPayload {
  settings: AppSettings;
}

export interface SettingsUpdatePayload {
  settings: Partial<AppSettings>;
}

// Authentication payloads
export interface AuthLoginPayload {
  username: string;
  password: string;
  mfaCode?: string;
}

export interface AuthMfaVerifyPayload {
  userId: string;
  mfaCode: string;
}

export interface AuthStatusPayload {
  isAuthenticated: boolean;
  userId?: string;
  username?: string;
  role?: 'admin' | 'operator' | 'viewer';
  sessionExpiry?: string;
  requiresMfa?: boolean;
}

export interface AuthLogoutPayload {
  success: boolean;
}

// Service status payloads
export interface ServiceStatusPayload {
  backendConnected: boolean;
  serviceRunning: boolean;
  lastHeartbeat: string;
  version: string;
  uptime: number;
}

export interface ServiceHealthPayload {
  status: 'healthy' | 'degraded' | 'unhealthy';
  components: {
    database: 'ok' | 'warning' | 'error';
    policyEngine: 'ok' | 'warning' | 'error';
    deviceMonitor: 'ok' | 'warning' | 'error';
    alerting: 'ok' | 'warning' | 'error';
  };
  timestamp: string;
}

// Error payloads
export interface RendererErrorPayload {
  error: string;
  stack?: string;
  component: string;
  timestamp: string;
}

// ============================================================================
// Request/Response wrapper types
// ============================================================================

export interface IPCRequest<T> {
  messageId: string;
  channel: ChannelName;
  payload: T;
}

export interface IPCResponse<T> {
  messageId: string;
  success: boolean;
  data?: T;
  error?: string;
}

// ============================================================================
// Type guards for runtime validation
// ============================================================================

export function isValidChannel(channel: string): channel is ChannelName {
  return Object.values(IPC_CHANNELS).includes(channel as ChannelName);
}

export function isDeviceInfo(obj: unknown): obj is DeviceInfo {
  if (!obj || typeof obj !== 'object') return false;
  const device = obj as Record<string, unknown>;
  return (
    typeof device.deviceId === 'string' &&
    typeof device.deviceName === 'string' &&
    ['keyboard', 'mouse', 'storage', 'network', 'other'].includes(device.deviceType as string) &&
    typeof device.vendorId === 'string' &&
    typeof device.productId === 'string' &&
    typeof device.isConnected === 'boolean' &&
    typeof device.isAuthorized === 'boolean' &&
    ['low', 'medium', 'high', 'critical'].includes(device.riskLevel as string)
  );
}

export function isAlertEvent(obj: unknown): obj is AlertEvent {
  if (!obj || typeof obj !== 'object') return false;
  const alert = obj as Record<string, unknown>;
  return (
    typeof alert.alertId === 'string' &&
    typeof alert.type === 'string' &&
    ['low', 'medium', 'high', 'critical'].includes(alert.severity as string) &&
    typeof alert.title === 'string' &&
    typeof alert.description === 'string' &&
    typeof alert.timestamp === 'string' &&
    typeof alert.acknowledged === 'boolean' &&
    typeof alert.dismissed === 'boolean'
  );
}
