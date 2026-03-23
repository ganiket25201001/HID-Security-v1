"use strict";
/**
 * IPC Channel Definitions and Payload Interfaces
 *
 * This file defines all allowed IPC channels and their payload types.
 * Both main and renderer processes must use these types for validation.
 *
 * SECURITY: All IPC messages are validated against these types in the preload script.
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.IPC_CHANNELS = void 0;
exports.isValidChannel = isValidChannel;
exports.isDeviceInfo = isDeviceInfo;
exports.isAlertEvent = isAlertEvent;
// ============================================================================
// Channel Names - Single source of truth for all IPC channels
// ============================================================================
exports.IPC_CHANNELS = {
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
};
// ============================================================================
// Type guards for runtime validation
// ============================================================================
function isValidChannel(channel) {
    return Object.values(exports.IPC_CHANNELS).includes(channel);
}
function isDeviceInfo(obj) {
    if (!obj || typeof obj !== 'object')
        return false;
    const device = obj;
    return (typeof device.deviceId === 'string' &&
        typeof device.deviceName === 'string' &&
        ['keyboard', 'mouse', 'storage', 'network', 'other'].includes(device.deviceType) &&
        typeof device.vendorId === 'string' &&
        typeof device.productId === 'string' &&
        typeof device.isConnected === 'boolean' &&
        typeof device.isAuthorized === 'boolean' &&
        ['low', 'medium', 'high', 'critical'].includes(device.riskLevel));
}
function isAlertEvent(obj) {
    if (!obj || typeof obj !== 'object')
        return false;
    const alert = obj;
    return (typeof alert.alertId === 'string' &&
        typeof alert.type === 'string' &&
        ['low', 'medium', 'high', 'critical'].includes(alert.severity) &&
        typeof alert.title === 'string' &&
        typeof alert.description === 'string' &&
        typeof alert.timestamp === 'string' &&
        typeof alert.acknowledged === 'boolean' &&
        typeof alert.dismissed === 'boolean');
}
